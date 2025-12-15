using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.OptionTypes;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Catalog.Products;

public static partial class ProductModule
{
    public static partial class OptionTypes
    {
        public static class Manage
        {
            public record Parameter : Models.ProductOptionTypeParameter;
            public sealed record Request
            {
                public List<Parameter> Data { get; set; } = new List<Parameter>();
            }

            public sealed record Command(Guid ProductId, Request Request) : ICommand<Success>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(expression: x => x.ProductId).NotEmpty();
                    RuleForEach(m => m.Request.Data)
                        .SetValidator(new Models.ProductOptionTypeParameterValidator());
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

                    var requestedOptionTypes = command.Request.Data
                        .ToDictionary(m => m.OptionTypeId, m => m.Position);

                    var existingOptionTypes = product.ProductOptionTypes
                        .ToDictionary(pot => pot.OptionTypeId);

                    var toRemove = existingOptionTypes
                        .Where(x => !requestedOptionTypes.ContainsKey(x.Key))
                        .Select(x => x.Value)
                        .ToList();

                    foreach (var pot in toRemove)
                    {
                        var removeResult = product.RemoveOptionType(pot.OptionTypeId);
                        if (removeResult.IsError) return removeResult.FirstError;

                        unitOfWork.Context.Set<ProductOptionType>()
                            .Remove(removeResult.Value);
                    }

                    foreach (var requested in requestedOptionTypes)
                    {
                        if (!existingOptionTypes.TryGetValue(requested.Key, out var existing))
                        {
                            // ADD
                            var createResult = ProductOptionType.Create(
                                productId: command.ProductId,
                                optionTypeId: requested.Key,
                                position: requested.Value
                            );

                            if (createResult.IsError) return createResult.FirstError;

                            var addResult = product.AddOptionType(createResult.Value);
                            if (addResult.IsError) return addResult.FirstError;

                            unitOfWork.Context.Set<ProductOptionType>()
                                .Add(createResult.Value);
                        }
                        else
                        {
                            // UPDATE
                            var updateResult = existing.UpdatePosition(requested.Value);
                            if (updateResult.IsError)
                                return updateResult.FirstError;
                            unitOfWork.Context.Set<ProductOptionType>().Update(updateResult.Value);
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