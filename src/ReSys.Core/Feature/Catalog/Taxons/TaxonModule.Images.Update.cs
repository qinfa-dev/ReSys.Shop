using MapsterMapper;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using ReSys.Core.Domain.Catalog.Taxonomies.Images;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;
using ReSys.Core.Feature.Common.Persistence.Interfaces;
using ReSys.Core.Feature.Common.Storage.Services;

namespace ReSys.Core.Feature.Catalog.Taxons;

public static partial class TaxonModule
{
    public static partial class Images
    {
        public static class Update
        {
            public sealed class Request
            {
                [FromForm(Name = "images")] public List<Models.ImageParameter> Images { get; set; } = new();
            }

            public sealed record Result : Models.ImageItem;
            public sealed record Command(Guid TaxonId, Request Request) : ICommand<List<Result>>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(expression: x => x.TaxonId).NotEmpty();
                    RuleFor(expression: x => x.Request.Images).NotNull().NotEmpty();
                    RuleForEach(expression: x => x.Request.Images).SetValidator(validator: new Models.ImageParameterValidator());
                }
            }

            public sealed class CommandHandler(
                IUnitOfWork unitOfWork,
                IMapper mapper,
                IStorageService storageService,
                ILogger<CommandHandler> logger) : ICommandHandler<Command, List<Result>>
            {
                public async Task<ErrorOr<List<Result>>> Handle(Command command, CancellationToken ct)
                {
                    await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                    try
                    {
                        var taxon = await unitOfWork.Context.Set<Taxon>()
                            .Include(navigationPropertyPath: t => t.TaxonImages)
                            .Include(navigationPropertyPath: t => t.Taxonomy)
                            .Include(navigationPropertyPath: t => t.Parent)
                            .Include(navigationPropertyPath: t => t.Children)
                            .FirstOrDefaultAsync(predicate: t => t.Id == command.TaxonId, cancellationToken: ct);

                        if (taxon == null)
                            return Taxon.Errors.NotFound(id: command.TaxonId);

                        var existingImages = taxon.TaxonImages.ToDictionary(keySelector: i => i.Id);
                        var incomingIds = command.Request.Images
                            .Where(predicate: p => p.Id.HasValue)
                            .Select(selector: p => p.Id!.Value)
                            .ToHashSet();

                        var filesToDelete = new List<string>();

                        foreach (var param in command.Request.Images)
                        {
                            if (param.Id.HasValue && existingImages.TryGetValue(key: param.Id.Value, value: out var existing))
                            {
                                if (param.File != null)
                                {
                                    var upload = await storageService.UploadFileAsync(file: param.File, path: $"taxons/{command.TaxonId}", cancellationToken: ct);
                                    if (upload.IsError) return upload.Errors;

                                    if (!string.IsNullOrEmpty(value: existing.Url))
                                        filesToDelete.Add(item: existing.Url);

                                    var updateResult = existing.Update(type: param.Type, url: upload.Value.Url, alt: param.Alt ?? param.File.FileName);
                                    if (updateResult.IsError) return updateResult.Errors;
                                }
                                else
                                {
                                    var updateResult = existing.Update(type: param.Type, url: existing.Url, alt: param.Alt ?? existing.Alt);
                                    if (updateResult.IsError) return updateResult.Errors;
                                }

                                if (existing.Position != param.Position)
                                    existing.SetPosition(position: param.Position);
                            }
                            else if (!param.Id.HasValue && param.File != null)
                            {
                                var upload = await storageService.UploadFileAsync(file: param.File, path: $"taxons/{command.TaxonId}", cancellationToken: ct);
                                if (upload.IsError) return upload.Errors;

                                var createResult = TaxonImage.Create(
                                    taxonId: command.TaxonId, type: param.Type, url: upload.Value.Url,
                                    alt: param.Alt ?? param.File.FileName, position: param.Position);

                                if (createResult.IsError) return createResult.Errors;
                                taxon.TaxonImages.Add(item: createResult.Value);
                            }
                        }

                        foreach (var existing in existingImages.Values)
                        {
                            if (!incomingIds.Contains(item: existing.Id))
                            {
                                if (!string.IsNullOrEmpty(value: existing.Url))
                                    filesToDelete.Add(item: existing.Url);

                                taxon.TaxonImages.Remove(item: existing);
                                unitOfWork.Context.Set<TaxonImage>().Remove(entity: existing);
                            }
                        }

                        await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                        await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                        foreach (var url in filesToDelete)
                        {
                            var del = await storageService.DeleteFileAsync(fileUrl: url, cancellationToken: ct);
                            if (del.IsError)
                                logger.LogWarning(message: "Failed to delete old taxon image: {Url}", args: url);
                        }

                        return mapper.Map<List<Result>>(source: existingImages);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(exception: ex, message: "Failed to update taxon images for {TaxonId}", args: command.TaxonId);
                        await unitOfWork.RollbackTransactionAsync(cancellationToken: ct);
                        return Error.Failure(code: "TaxonImage.UpdateFailed", description: "Failed to update images.");
                    }
                }
            }
        }
    }
}