using MapsterMapper;

using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Inventories.StockTransfers;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Inventories.StockTransfers;

public static partial class StockTransferModule
{
    // Create
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

                // Validate locations exist
                var destinationLocation = await unitOfWork.Context.Set<StockLocation>()
                    .FindAsync(keyValues: [param.DestinationLocationId], cancellationToken: ct);
                if (destinationLocation == null)
                    return StockTransfer.Errors.StockLocationNotFound(locationId: param.DestinationLocationId);

                if (param.SourceLocationId.HasValue)
                {
                    var sourceLocation = await unitOfWork.Context.Set<StockLocation>()
                        .FindAsync(keyValues: [param.SourceLocationId.Value], cancellationToken: ct);
                    if (sourceLocation == null)
                        return StockTransfer.Errors.StockLocationNotFound(locationId: param.SourceLocationId.Value);
                }

                var createResult = StockTransfer.Create(
                    destinationLocationId: param.DestinationLocationId,
                    sourceLocationId: param.SourceLocationId,
                    reference: param.Reference);

                if (createResult.IsError) return createResult.Errors;

                unitOfWork.Context.Set<StockTransfer>().Add(entity: createResult.Value);
                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return mapper.Map<Result>(source: createResult.Value);
            }
        }
    }
}