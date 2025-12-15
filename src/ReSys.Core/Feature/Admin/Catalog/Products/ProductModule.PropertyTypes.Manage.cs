using Mapster;

using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.PropertyTypes;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Catalog.Products;

public static partial class ProductModule
{
    public static partial class PropertyType
    {
        public static class Manage
        {
            public record Parameter : Models.ProductPropertyParameter
            {
                public Guid PropertyId { get; set; }
            } 
            
            public sealed record Request
            {
                public List<Parameter> Data { get; init; } = new();
            }
            public sealed record Result : Models.ProductPropertyResult;
            public sealed record Command(Guid ProductId, Request Request) : ICommand<List<Result>>;

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
                            property.RuleFor(expression: p => p.PropertyValue).NotEmpty();
                        });
                }
            }

            public sealed class CommandHandler(IUnitOfWork unitOfWork)
                : ICommandHandler<Command, List<Result>>
            {
                public async Task<ErrorOr<List<Result>>> Handle(Command request, CancellationToken ct)
                {
                    var product = await unitOfWork.Context.Set<Product>()
                        .Include(navigationPropertyPath: p => p.ProductPropertyTypes)
                        .FirstOrDefaultAsync(predicate: p => p.Id == request.ProductId, cancellationToken: ct);

                    if (product == null)
                        return Product.Errors.NotFound(id: request.ProductId);

                    await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                    var requestPropertyIds = request.Request.Data.Select(selector: p => p.PropertyId).ToHashSet();

                    var properties = await unitOfWork.Context.Set<Domain.Catalog.PropertyTypes.PropertyType>()
                        .Where(predicate: pt => requestPropertyIds.Contains(pt.Id))
                        .ToListAsync(cancellationToken: ct);

                    var missingPropertyIds = requestPropertyIds.Except(second: properties.Select(selector: p => p.Id)).ToList();
                    if (missingPropertyIds.Any())
                    {
                        return missingPropertyIds.Select(selector: Domain.Catalog.PropertyTypes.PropertyType.Errors.NotFound).ToList();
                    }
                    var toRemove = product.ProductPropertyTypes
                        .Where(predicate: pp => !requestPropertyIds.Contains(item: pp.PropertyTypeId))
                        .ToList();

                    foreach (var pp in toRemove)
                    {
                        product.ProductPropertyTypes.Remove(item: pp);
                    }

                    foreach (var propertyValue in request.Request.Data)
                    {
                        var productProperty = product.ProductPropertyTypes.FirstOrDefault(predicate: pp => pp.PropertyTypeId == propertyValue.PropertyId);
                        if (productProperty is not null)
                        {
                            var updateResult = productProperty
                                .Update(value: propertyValue.PropertyValue, position: propertyValue.Position);

                            if (updateResult.IsError)
                                return updateResult.FirstError;

                            unitOfWork.Context.Set<ProductPropertyType>().Update(updateResult.Value);
                        }
                        else
                        {
                            var createResult = ProductPropertyType.Create(
                                productId: request.ProductId,
                                propertyId: propertyValue.PropertyId,
                                value: propertyValue.PropertyValue,
                                position: propertyValue.Position);

                            if (createResult.IsError)
                                return createResult.FirstError;

                            product.ProductPropertyTypes.Add(item: createResult.Value);
                            unitOfWork.Context.Set<ProductPropertyType>().Add(createResult.Value);
                        }
                    }
                    await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                    await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                    return product.ProductOptionTypes
                        .Select(m => m.Adapt<Result>())
                        .ToList();
                }
            }
        }
    }
}