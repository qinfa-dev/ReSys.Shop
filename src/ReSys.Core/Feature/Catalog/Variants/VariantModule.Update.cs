using MapsterMapper;

using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.Variants;

public static partial class VariantModule
{
    public static class Update
    {
        public record Request : Models.Parameter;
        public record Result : Models.ListItem;
        public sealed record Command(Guid Id, Request Request) : ICommand<Result>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Id).NotEmpty();
                RuleFor(expression: x => x.Request).SetValidator(validator: new Models.ParameterValidator());
            }
        }

        public sealed class CommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
            : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken ct)
            {
                var request = command.Request;
                var variant = await unitOfWork.Context.Set<Variant>()
                    .FindAsync(keyValues: [command.Id], cancellationToken: ct);
                if (variant == null)
                    return Variant.Errors.NotFound(id: command.Id);

                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                var updateResult = variant.Update(
                    sku: request.Sku,
                    barcode: request.Barcode,
                    weight: request.Weight,
                    height: request.Height,
                    width: request.Width,
                    depth: request.Depth,
                    dimensionsUnit: request.DimensionsUnit,
                    weightUnit: request.WeightUnit,
                    trackInventory: request.TrackInventory,
                    costPrice: request.CostPrice,
                    costCurrency: request.CostCurrency,
                    position: request.Position,
                    publicMetadata: request.PublicMetadata,
                    privateMetadata: request.PrivateMetadata);

                if (updateResult.IsError) return updateResult.Errors;
                
                unitOfWork.Context.Set<Variant>().Update(updateResult.Value);
                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return mapper.Map<Result>(source: updateResult.Value);
            }
        }
    }
}