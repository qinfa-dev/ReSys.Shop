using MapsterMapper;

using Microsoft.Extensions.Logging;

using ReSys.Core.Domain.Catalog.Products.Images;
using ReSys.Core.Feature.Common.Persistence.Interfaces;
using ReSys.Core.Feature.Common.Storage.Models;
using ReSys.Core.Feature.Common.Storage.Services;

namespace ReSys.Core.Feature.Catalog.Products;

public static partial class ProductModule
{
    public static partial class Images
    {
        public static class Edit
        {
            public sealed class Request : Models.UploadImageParameter;
            public sealed class Result : Models.ImageResult;

            public sealed record Command(
                Guid ProductId,
                Guid ImageId,
                Request Request
            ) : ICommand<Result>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(x => x.ProductId).NotEmpty();
                    RuleFor(x => x.ImageId).NotEmpty();
                    RuleFor(x => x.Request)
                        .SetValidator(new Models.UploadImageParameterValidator());
                }
            }

            public sealed class CommandHandler(
                IUnitOfWork unitOfWork,
                IMapper mapper,
                IStorageService storageService,
                ILogger<CommandHandler> logger
            ) : ICommandHandler<Command, Result>
            {
                public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken ct)
                {
                    await unitOfWork.BeginTransactionAsync(ct);

                    string? oldUrl = null;

                    try
                    {
                        var image = await unitOfWork.Context.Set<ProductImage>()
                            .FirstOrDefaultAsync(
                                i => i.Id == command.ImageId
                                     && i.ProductId == command.ProductId
                                     && (command.Request.VariantId == null || i.VariantId == command.Request.VariantId.Value),
                                ct);

                        if (image is null)
                            return ProductImage.Errors.NotFound(command.ImageId);

                        // -----------------------------------------
                        // CASE 1: File replacement
                        // -----------------------------------------
                        if (command.Request.File is not null)
                        {
                            var imageType =
                                Enum.Parse<ProductImage.ProductImageType>(
                                    command.Request.Type,
                                    ignoreCase: true);

                            var uploadOptions =
                                UploadOptions.FromDomainSpec(
                                    type: imageType,
                                    productId: command.ProductId,
                                    variantId: command.Request.VariantId,
                                    contentType: command.Request.File.ContentType);

                            var uploadResult = await storageService.UploadFileAsync(
                                command.Request.File,
                                uploadOptions,
                                ct);

                            if (uploadResult.IsError)
                                return uploadResult.Errors;

                            oldUrl = image.Url;

                            var updateResult = image.Update(
                                variantId: command.Request.VariantId,
                                url: uploadResult.Value.Url,
                                alt: command.Request.Alt ?? command.Request.File.FileName,
                                position: command.Request.Position,
                                type: command.Request.Type,
                                contentType: uploadResult.Value.ContentType,
                                width: uploadResult.Value.Width,
                                height: uploadResult.Value.Height
                            );

                            if (updateResult.IsError)
                            {
                                await storageService.DeleteFileAsync(
                                    uploadResult.Value.Url,
                                    ct);

                                return updateResult.Errors;
                            }
                        }
                        // -----------------------------------------
                        // CASE 2: Metadata-only update
                        // -----------------------------------------
                        else
                        {
                            var updateResult = image.Update(
                                alt: command.Request.Alt,
                                position: command.Request.Position,
                                type: command.Request.Type
                            );

                            if (updateResult.IsError)
                                return updateResult.Errors;
                        }

                        await unitOfWork.SaveChangesAsync(ct);
                        await unitOfWork.CommitTransactionAsync(ct);

                        // -----------------------------------------
                        // Post-commit cleanup
                        // -----------------------------------------
                        if (!string.IsNullOrWhiteSpace(oldUrl))
                        {
                            var deleteOld = await storageService.DeleteFileAsync(oldUrl, ct);

                            if (deleteOld.IsError)
                            {
                                logger.LogWarning(
                                    "Failed to delete old product image file: {Url}",
                                    oldUrl);
                            }
                        }

                        return mapper.Map<Result>(image);
                    }
                    catch (Exception ex)
                    {
                        await unitOfWork.RollbackTransactionAsync(ct);

                        logger.LogError(
                            ex,
                            "Failed to edit product image {ImageId}",
                            command.ImageId);

                        return Error.Failure(
                            code: "ProductImage.EditFailed",
                            description: "Failed to edit product image.");
                    }
                }
            }
        }
    }
}
