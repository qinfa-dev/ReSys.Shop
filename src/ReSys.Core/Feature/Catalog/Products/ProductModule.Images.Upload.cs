using MapsterMapper;

using Microsoft.Extensions.Logging;

using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.Images;
using ReSys.Core.Feature.Common.Persistence.Interfaces;
using ReSys.Core.Feature.Common.Storage.Models;
using ReSys.Core.Feature.Common.Storage.Services;

namespace ReSys.Core.Feature.Catalog.Products;

public static partial class ProductModule
{
    public static partial class Images
    {
        public static class Upload
        {
            public sealed class Request : Models.UploadImageParameter;
            public sealed class Result : Models.ImageResult;

            public sealed record Command(
                Guid ProductId,
                Request Request
            ) : ICommand<Result>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(x => x.ProductId).NotEmpty();
                    RuleFor(x => x.Request)
                        .NotNull()
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

                    try
                    {
                        var productExists = await unitOfWork.Context.Set<Product>()
                            .AnyAsync(p => p.Id == command.ProductId, ct);

                        if (!productExists)
                            return Product.Errors.NotFound(command.ProductId);

                        var imageType =
                            Enum.Parse<ProductImage.ProductImageType>(
                                command.Request.Type,
                                ignoreCase: true);

                        // -----------------------------------------
                        // Prevent duplicate image type (e.g. Default)
                        // -----------------------------------------
                        if (imageType == ProductImage.ProductImageType.Default)
                        {
                            bool exists = await unitOfWork.Context.Set<ProductImage>()
                                .AnyAsync(
                                    i => i.ProductId == command.ProductId
                                         && (command.Request.VariantId == null || i.VariantId == command.Request.VariantId.Value)
                                         && i.Type == nameof(ProductImage.ProductImageType.Default),
                                    ct);

                            if (exists)
                                return ProductImage.Errors.AlreadyExists(
                                    command.ProductId,
                                    null,
                                    command.Request.Type);
                        }

                        // -----------------------------------------
                        // Build upload options from DOMAIN SPEC
                        // -----------------------------------------
                        var uploadOptions =
                            UploadOptions.FromDomainSpec(
                                type: imageType,
                                productId: command.ProductId,
                                variantId: command.Request.VariantId,
                                contentType: command.Request.File!.ContentType);

                        var uploadResult = await storageService.UploadFileAsync(
                            command.Request.File,
                            uploadOptions,
                            ct);

                        if (uploadResult.IsError)
                            return uploadResult.Errors;

                        // -----------------------------------------
                        // Create domain entity
                        // -----------------------------------------
                        var createResult = ProductImage.Create(
                            url: uploadResult.Value.Url,
                            productId: command.ProductId,
                            variantId: command.Request.VariantId,
                            alt: command.Request.Alt ?? command.Request.File.FileName,
                            position: command.Request.Position,
                            type: command.Request.Type,
                            contentType: uploadResult.Value.ContentType,
                            width: uploadResult.Value.Width,
                            height: uploadResult.Value.Height
                        );

                        if (createResult.IsError)
                        {
                            await storageService.DeleteFileAsync(
                                uploadResult.Value.Url,
                                ct);

                            return createResult.Errors;
                        }

                        unitOfWork.Context.Set<ProductImage>().Add(createResult.Value);
                        await unitOfWork.SaveChangesAsync(ct);
                        await unitOfWork.CommitTransactionAsync(ct);

                        // -----------------------------------------
                        // Map result
                        // -----------------------------------------
                        var result = mapper.Map<Result>(createResult.Value);
                        result.Size = uploadResult.Value.Length;
                        result.ContentType = uploadResult.Value.ContentType;
                        result.Width = uploadResult.Value.Width;
                        result.Height = uploadResult.Value.Height;
                        result.Thumbnails = uploadResult.Value.Thumbnails;

                        return result;
                    }
                    catch (Exception ex)
                    {
                        await unitOfWork.RollbackTransactionAsync(ct);

                        logger.LogError(
                            ex,
                            "Failed to upload product image for {ProductId}",
                            command.ProductId);

                        return Error.Failure(
                            code: "ProductImage.UploadFailed",
                            description: "Failed to upload product image.");
                    }
                }
            }
        }
    }
}
