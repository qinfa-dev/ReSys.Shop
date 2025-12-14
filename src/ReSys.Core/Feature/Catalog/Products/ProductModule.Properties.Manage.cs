using Mapster;

using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.PropertyTypes;
using ReSys.Core.Domain.Catalog.PropertyTypes;
using ReSys.Core.Feature.Catalog.PropertyTypes;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.Products;

public static partial class ProductModule
{
    public static partial class Properties
    {
        public static class Manage
        {
            public record Parameter : Models.PropertyParameter
            {
                public Guid PropertyId { get; set; }
            } 
            
            public sealed record Request
            {
                public List<Parameter> Data { get; init; } = new();
            }
            public sealed record SelectItemResult : PropertyTypeModule.Models.SelectItem;
            public sealed record Command(Guid ProductId, Request Request) : ICommand<List<SelectItemResult>>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(expression: x => x.ProductId).NotEmpty();
                    RuleFor(expression: x => x.Request.Data).NotNull();
                    RuleForEach(expression: x => x.Request.Data)
                        .ChildRules(action: property =>
                        {
                            property.RuleFor(expression: p => p.PropertyId).NotEmpty();
                            property.RuleFor(expression: p => p.Value).NotEmpty();
                        });
                }
            }

            public sealed class CommandHandler(IUnitOfWork unitOfWork)
                : ICommandHandler<Command, List<SelectItemResult>>
            {
                public async Task<ErrorOr<List<SelectItemResult>>> Handle(Command request, CancellationToken ct)
                {
                    var product = await unitOfWork.Context.Set<Product>()
                        .Include(navigationPropertyPath: p => p.ProductProperties)
                        .FirstOrDefaultAsync(predicate: p => p.Id == request.ProductId, cancellationToken: ct);

                    if (product == null)
                        return Product.Errors.NotFound(id: request.ProductId);

                    await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                    var requestPropertyIds = request.Request.Data.Select(selector: p => p.PropertyId).ToHashSet();

                    var properties = await unitOfWork.Context.Set<PropertyType>()
                        .Where(predicate: pt => requestPropertyIds.Contains(pt.Id))
                        .ToListAsync(cancellationToken: ct);
                    var missingPropertyIds = requestPropertyIds.Except(second: properties.Select(selector: p => p.Id)).ToList();
                    if (missingPropertyIds.Any())
                    {
                        return missingPropertyIds.Select(selector: PropertyType.Errors.NotFound).ToList();
                    }
                    var toRemove = product.ProductProperties
                        .Where(predicate: pp => !requestPropertyIds.Contains(item: pp.PropertyId))
                        .ToList();

                    foreach (var pp in toRemove)
                    {
                        product.ProductProperties.Remove(item: pp);
                    }

                    foreach (var propertyValue in request.Request.Data)
                    {
                        var productProperty = product.ProductProperties.FirstOrDefault(predicate: pp => pp.PropertyId == propertyValue.PropertyId);
                        if (productProperty is not null)
                        {
                            var updateResult = productProperty
                                .Update(value: propertyValue.Value, position: propertyValue.Position);

                            if (updateResult.IsError)
                                return updateResult.FirstError;

                            unitOfWork.Context.Set<ProductPropertyType>().Update(updateResult.Value);
                        }
                        else
                        {
                            var createResult = ProductPropertyType.Create(
                                productId: request.ProductId,
                                propertyId: propertyValue.PropertyId,
                                value: propertyValue.Value,
                                position: propertyValue.Position);

                            if (createResult.IsError)
                                return createResult.FirstError;

                            product.ProductProperties.Add(item: createResult.Value);
                            unitOfWork.Context.Set<ProductPropertyType>().Add(createResult.Value);
                        }
                    }
                    await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                    await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                    return product.ProductOptionTypes
                        .Select(m => m.Adapt<SelectItemResult>())
                        .ToList();
                }
            }
        }
    }
}