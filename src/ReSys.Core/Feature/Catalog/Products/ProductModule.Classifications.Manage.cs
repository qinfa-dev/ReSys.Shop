using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.Classifications;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.Products;

public static partial class ProductModule
{
    public static partial class Classifications
    {
        public static class Manage
        {
            public sealed record Request
            {
                public List<Guid> TaxonIds { get; init; } = new();
            }

            public sealed record Command(Guid ProductId, Request Request) : ICommand<Success>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(expression: x => x.ProductId).NotEmpty();
                    RuleFor(expression: x => x.Request.TaxonIds).NotNull();

                    var idRequired = CommonInput.Errors.Required(prefix: nameof(Classification),
                        field: nameof(Classification.TaxonId));
                    RuleForEach(expression: x => x.Request.TaxonIds)
                        .NotEmpty()
                        .WithMessage(idRequired.Description)
                        .WithErrorCode(idRequired.Code);
                }
            }

            public sealed class CommandHandler(IUnitOfWork unitOfWork)
                : ICommandHandler<Command, Success>
            {
                public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken ct)
                {
                    var product = await unitOfWork.Context.Set<Product>()
                        .Include(navigationPropertyPath: p => p.Classifications)
                        .FirstOrDefaultAsync(predicate: p => p.Id == command.ProductId, cancellationToken: ct);

                    if (product == null)
                        return Product.Errors.NotFound(id: command.ProductId);

                    await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                    // Remove classifications not in the new list
                    var toRemove = product.Classifications
                        .Where(predicate: c => !command.Request.TaxonIds.Contains(item: c.TaxonId))
                        .ToList();

                    foreach (var classification in toRemove)
                    {
                        product.Classifications.Remove(item: classification);
                    }

                    // Add new classifications
                    var existingIds = product.Classifications.Select(selector: c => c.TaxonId).ToHashSet();
                    foreach (var taxonId in command.Request.TaxonIds)
                    {
                        if (!existingIds.Contains(item: taxonId))
                        {
                            var createResult = Classification.Create(productId: command.ProductId, taxonId: taxonId);
                            if (createResult.IsError) return createResult.FirstError;

                            product.Classifications.Add(item: createResult.Value);
                        }
                    }

                    await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                    await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                    return Result.Success;
                }
            }
        }
    }
}