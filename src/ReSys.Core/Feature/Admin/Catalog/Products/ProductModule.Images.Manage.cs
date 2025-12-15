using MapsterMapper;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.Images;
using ReSys.Core.Feature.Common.Persistence.Interfaces;
using ReSys.Core.Feature.Common.Storage.Models;
using ReSys.Core.Feature.Common.Storage.Services;

namespace ReSys.Core.Feature.Admin.Catalog.Products;

public static partial class ProductModule
{
    public static partial class Images
    {
        public static class Manage
        {
            public class Parameter : Models.UploadImageParameter
            {
                [FromForm(Name = "image_id")] public Guid? ImageId { get; init; }
            }
            public sealed class Request
            {
                [FromForm(Name = "data")] public List<Parameter> Data { get; set; } = new();
            }

            public sealed class Result : Models.ImageResult;

            public sealed record Command(Guid ProductId, Request Request) : ICommand<List<Result>>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(expression: x => x.ProductId).NotEmpty();
                    RuleFor(expression: x => x.Request.Data).NotNull().NotEmpty();
                    RuleForEach(expression: x => x.Request.Data)
                        .SetValidator(validator: new Models.ImageParameterValidator());
                }
            }

            public sealed class CommandHandler(
                IUnitOfWork unitOfWork,
                IMapper mapper,
                IStorageService storageService,
                ILogger<CommandHandler> logger)
                : ICommandHandler<Command, List<Result>>
            {
                public async Task<ErrorOr<List<Result>>> Handle(Command command, CancellationToken ct)
                {
                    await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                    try
                    {
                        var product = await unitOfWork.Context.Set<Product>()
                            .Include(navigationPropertyPath: p => p.Images)
                            .FirstOrDefaultAsync(predicate: p => p.Id == command.ProductId, cancellationToken: ct);

                        if (product == null)
                            return Product.Errors.NotFound(id: command.ProductId);

                        var existingImages = product.Images.Where(predicate: i => i.VariantId == null)
                            .ToDictionary(keySelector: i => i.Id);
                        var incomingIds = command.Request.Data
                            .Where(predicate: p => p.ImageId.HasValue)
                            .Select(selector: p => p.ImageId!.Value)
                            .ToHashSet();

                        var filesToDelete = new List<string>();

                        foreach (var param in command.Request.Data)
                        {
                            if (param.ImageId.HasValue &&
                                existingImages.TryGetValue(key: param.ImageId.Value, value: out var existing))
                            {
                                if (param.File != null)
                                {
                                    var upload = await storageService.UploadFileAsync(file: param.File,
                                        options: new UploadOptions { Folder = $"products/{command.ProductId}" }, cancellationToken: ct);
                                    if (upload.IsError) return upload.Errors;

                                    if (!string.IsNullOrEmpty(value: existing.Url))
                                        filesToDelete.Add(item: existing.Url);

                                    var updateResult = existing.Update(url: upload.Value.Url,
                                        alt: param.Alt ?? param.File.FileName);
                                    if (updateResult.IsError) return updateResult.Errors;
                                }
                                else
                                {
                                    var updateResult = existing.Update(url: existing.Url,
                                        alt: param.Alt ?? existing.Alt);
                                    if (updateResult.IsError) return updateResult.Errors;
                                }

                                if (existing.Position != param.Position)
                                    existing.SetPosition(position: param.Position);
                            }
                            else if (!param.ImageId.HasValue && param.File != null)
                            {
                                var upload = await storageService.UploadFileAsync(file: param.File,
                                    options: new UploadOptions { Folder = $"products/{command.ProductId}" }, cancellationToken: ct);
                                if (upload.IsError) return upload.Errors;

                                var createResult = ProductImage.Create(
                                    productId: command.ProductId,
                                    variantId: null,
                                    type: param.Type,
                                    url: upload.Value.Url,
                                    alt: param.Alt ?? param.File.FileName,
                                    position: param.Position);

                                if (createResult.IsError) return createResult.Errors;
                                product.Images.Add(item: createResult.Value);
                            }
                        }

                        foreach (var existing in existingImages.Values)
                        {
                            if (!incomingIds.Contains(item: existing.Id))
                            {
                                if (!string.IsNullOrEmpty(value: existing.Url))
                                    filesToDelete.Add(item: existing.Url);

                                product.Images.Remove(item: existing);
                                unitOfWork.Context.Set<ProductImage>().Remove(entity: existing);
                            }
                        }

                        await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                        await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                        foreach (var url in filesToDelete)
                        {
                            var del = await storageService.DeleteFileAsync(fileUrl: url, cancellationToken: ct);
                            if (del.IsError)
                                logger.LogWarning(message: "Failed to delete old product image: {Url}", args: url);
                        }

                        var resultImages = product.Images.Where(predicate: i => i.VariantId == null)
                            .Select(selector: mapper.Map<Result>)
                            .ToList();

                        return resultImages;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(exception: ex, message: "Failed to update product images for {ProductId}",
                            args: command.ProductId);
                        await unitOfWork.RollbackTransactionAsync(cancellationToken: ct);
                        return Error.Failure(code: "ProductImage.UpdateFailed",
                            description: "Failed to update images.");
                    }
                }
            }
        }
    }
}