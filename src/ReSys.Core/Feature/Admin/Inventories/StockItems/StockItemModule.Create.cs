using MapsterMapper;

using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Inventories.Stocks;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Inventories.StockItems;

public static partial class StockItemModule
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

                // Validate Variant exists
                var variant = await unitOfWork.Context.Set<Variant>()
                    .FindAsync(keyValues: [param.VariantId], cancellationToken: ct);
                if (variant == null)
                    return Variant.Errors.NotFound(id: param.VariantId);

                // Validate StockLocation exists
                var location = await unitOfWork.Context.Set<StockLocation>()
                    .FindAsync(keyValues: [param.StockLocationId], cancellationToken: ct);
                if (location == null)
                    return StockLocation.Errors.NotFound(id: param.StockLocationId);

                // Check for duplicate SKU in the same location
                var duplicate = await unitOfWork.Context.Set<StockItem>()
                    .AnyAsync(predicate: si => si.Sku == param.Sku && si.StockLocationId == param.StockLocationId, cancellationToken: ct);
                if (duplicate)
                    return StockItem.Errors.DuplicateSku(sku: param.Sku, stockLocationId: param.StockLocationId);

                var createResult = StockItem.Create(
                    variantId: param.VariantId,
                    stockLocationId: param.StockLocationId,
                    sku: param.Sku,
                    quantityOnHand: param.QuantityOnHand,
                    quantityReserved: param.QuantityReserved,
                    backorderable: param.Backorderable,
                    publicMetadata: param.PublicMetadata,
                    privateMetadata: param.PrivateMetadata);

                if (createResult.IsError) return createResult.Errors;

                unitOfWork.Context.Set<StockItem>().Add(entity: createResult.Value);
                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return mapper.Map<Result>(source: createResult.Value);
            }
        }
    }
}