using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.OptionTypes;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.Products;

public static partial class ProductModule
{
    public static partial class OptionTypes
    {
        public static class Manage
        {
            public sealed record Request
            {
                public List<Guid> OptionTypeIds { get; set; } = new List<Guid>();
            }

            public sealed record Command(Guid ProductId, Request Request) : ICommand<Success>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(expression: x => x.ProductId).NotEmpty();
                    RuleFor(expression: x => x.Request.OptionTypeIds).NotNull();
                }
            }

            public sealed class CommandHandler(IUnitOfWork unitOfWork)
                : ICommandHandler<Command, Success>
            {
                public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken ct)
                {
                    var product = await unitOfWork.Context.Set<Product>()
                        .Include(navigationPropertyPath: p => p.ProductOptionTypes)
                        .FirstOrDefaultAsync(predicate: p => p.Id == command.ProductId, cancellationToken: ct);

                    if (product == null)
                        return Product.Errors.NotFound(id: command.ProductId);

                    await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                    // Remove option types not in the new list
                    var toRemove = product.ProductOptionTypes
                        .Where(predicate: pot => !command.Request.OptionTypeIds.Contains(item: pot.OptionTypeId))
                        .ToList();

                    foreach (var pot in toRemove)
                    {
                        product.ProductOptionTypes.Remove(item: pot);
                    }

                    // Add new option types
                    var existingIds = product.ProductOptionTypes.Select(selector: pot => pot.OptionTypeId).ToHashSet();
                    foreach (var optionTypeId in command.Request.OptionTypeIds)
                    {
                        if (!existingIds.Contains(item: optionTypeId))
                        {
                            var createResult = ProductOptionType.Create(productId: command.ProductId, optionTypeId: optionTypeId);
                            if (createResult.IsError) return createResult.FirstError;

                            product.ProductOptionTypes.Add(item: createResult.Value);
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