using MapsterMapper;

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
                        if (!Enum.TryParse<ProductImage.ProductImageType>(
                                command.Request.Type,
                                ignoreCase: true,
                                out var imageType))
                        {
                            await unitOfWork.RollbackTransactionAsync(ct);
                            return Error.Validation("Image.Type.Invalid", "Invalid image type.");
                        }

                        var product = await unitOfWork.Context.Set<Product>()
                            .Include(p => p.Variants)
                            .FirstOrDefaultAsync(p => p.Id == command.ProductId, ct);

                        if (product == null)
                        {
                            await unitOfWork.RollbackTransactionAsync(ct);
                            return Product.Errors.NotFound(command.ProductId);
                        }

                        var variantId =
                            command.Request.VariantId ??
                            product.Variants.FirstOrDefault(v => v.IsMaster)?.Id;

                        if (imageType is ProductImage.ProductImageType.Default
                            or ProductImage.ProductImageType.Search)
                        {
                            bool exists = await unitOfWork.Context.Set<ProductImage>()
                                .AnyAsync(
                                    i => i.ProductId == command.ProductId
                                         && i.VariantId == variantId
                                         && i.Type == imageType.ToString(),
                                    ct);

                            if (exists)
                            {
                                await unitOfWork.RollbackTransactionAsync(ct);
                                return ProductImage.Errors.AlreadyExists(
                                    command.ProductId,
                                    variantId,
                                    command.Request.Type);
                            }
                        }

                        var uploadOptions = UploadOptions.FromDomainSpec(
                            type: imageType,
                            productId: command.ProductId,
                            variantId: variantId,
                            contentType: command.Request.File!.ContentType);

                        var uploadResult = await storageService.UploadFileAsync(
                            command.Request.File,
                            uploadOptions,
                            ct);

                        if (uploadResult.IsError)
                        {
                            await unitOfWork.RollbackTransactionAsync(ct);
                            return uploadResult.Errors;
                        }

                        var createResult = ProductImage.Create(
                            url: uploadResult.Value.Url,
                            productId: command.ProductId,
                            variantId: variantId,
                            alt: command.Request.Alt ?? command.Request.File.FileName,
                            position: command.Request.Position,
                            type: command.Request.Type,
                            contentType: uploadResult.Value.ContentType,
                            width: uploadResult.Value.Width,
                            height: uploadResult.Value.Height);

                        if (createResult.IsError)
                        {
                            await storageService.DeleteFileAsync(uploadResult.Value.Url, ct);
                            await unitOfWork.RollbackTransactionAsync(ct);
                            return createResult.Errors;
                        }

                        unitOfWork.Context.Set<ProductImage>().Add(createResult.Value);
                        await unitOfWork.SaveChangesAsync(ct);
                        await unitOfWork.CommitTransactionAsync(ct);

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
