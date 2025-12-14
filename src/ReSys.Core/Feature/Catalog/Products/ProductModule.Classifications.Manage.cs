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
            public record Parameter
                : Models.ProductClassificationParameter;

            public sealed record Request
            {
                public List<Parameter> Data { get; set; } = new();
            }

            public sealed record Command(Guid ProductId, Request Request)
                : ICommand<Success>;

            public sealed class CommandValidator
                : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(x => x.ProductId).NotEmpty();
                    RuleForEach(x => x.Request.Data)
                        .SetValidator(new Models.ProductClassificationParameterValidator());
                }
            }

            public sealed class CommandHandler(IUnitOfWork unitOfWork)
                : ICommandHandler<Command, Success>
            {
                public async Task<ErrorOr<Success>> Handle(
                    Command command,
                    CancellationToken ct)
                {
                    var product = await unitOfWork.Context.Set<Product>()
                        .Include(p => p.Classifications)
                        .FirstOrDefaultAsync(
                            p => p.Id == command.ProductId,
                            ct);

                    if (product == null)
                        return Product.Errors.NotFound(command.ProductId);

                    await unitOfWork.BeginTransactionAsync(ct);

                    var requested = command.Request.Data
                        .ToDictionary(x => x.TaxonId, x => x.Position);

                    var existing = product.Classifications
                        .ToDictionary(x => x.TaxonId);

                    // REMOVE
                    var toRemove = existing
                        .Where(x => !requested.ContainsKey(x.Key))
                        .Select(x => x.Value)
                        .ToList();

                    foreach (var classification in toRemove)
                    {
                        var remove = product.RemoveClassification(classification.TaxonId);
                        if (remove.IsError) return remove.FirstError;

                        unitOfWork.Context.Set<Classification>()
                            .Remove(remove.Value);
                    }

                    // ADD / UPDATE
                    foreach (var item in requested)
                    {
                        if (!existing.TryGetValue(item.Key, out var current))
                        {
                            var create = Classification.Create(
                                command.ProductId,
                                item.Key,
                                item.Value);

                            if (create.IsError) return create.FirstError;

                            var add = product.AddClassification(create.Value);
                            if (add.IsError) return add.FirstError;

                            unitOfWork.Context.Set<Classification>()
                                .Add(create.Value);
                        }
                        else
                        {
                            current.Position = item.Value;
                            unitOfWork.Context.Set<Classification>()
                                .Update(current);
                        }
                    }

                    await unitOfWork.SaveChangesAsync(ct);
                    await unitOfWork.CommitTransactionAsync(ct);

                    return Result.Success;
                }
            }
        }
    }
}
