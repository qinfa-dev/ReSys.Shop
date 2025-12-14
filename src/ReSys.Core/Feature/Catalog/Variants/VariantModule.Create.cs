using MapsterMapper;

using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.Variants;

public static partial class VariantModule
{
    public static class Create
    {
        public sealed record Request : Models.Parameter;
        public sealed record Result : Models.ListItem;
        public sealed record Command(Request Request) : ICommand<Result>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Request).SetValidator(validator: new Models.ParameterValidator());
            }
        }

        public class CommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
            : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken ct)
            {
                var param = command.Request;
                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                var product = await unitOfWork.Context.Set<Product>()
                    .FindAsync(keyValues: [param.ProductId], cancellationToken: ct);
                if (product == null)
                    return Product.Errors.NotFound(id: param.ProductId);

                var createResult = Variant.Create(
                    productId: param.ProductId,
                    isMaster: false,
                    sku: param.Sku,
                    barcode: param.Barcode,
                    weight: param.Weight,
                    height: param.Height,
                    width: param.Width,
                    depth: param.Depth,
                    dimensionsUnit: param.DimensionsUnit,
                    weightUnit: param.WeightUnit,
                    costPrice: param.CostPrice,
                    costCurrency: param.CostCurrency,
                    trackInventory: param.TrackInventory,
                    position: param.Position,
                    publicMetadata: param.PublicMetadata,
                    privateMetadata: param.PrivateMetadata);

                if (createResult.IsError) return createResult.Errors;
                product.Variants.Add(item: createResult.Value);

                unitOfWork.Context.Set<Variant>().Add(createResult.Value);
                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return mapper.Map<Result>(source: createResult.Value);
            }
        }
    }
}