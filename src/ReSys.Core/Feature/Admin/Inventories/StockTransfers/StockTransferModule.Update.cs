using MapsterMapper;

using ReSys.Core.Domain.Inventories.StockTransfers;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Inventories.StockTransfers;

public static partial class StockTransferModule
{
    // Update
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
                var transfer = await unitOfWork.Context.Set<StockTransfer>()
                    .FindAsync(keyValues: [command.Id], cancellationToken: ct);

                if (transfer == null)
                    return StockTransfer.Errors.NotFound(id: command.Id);

                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                var updateResult = transfer.Update(
                    destinationLocationId: request.DestinationLocationId,
                    sourceLocationId: request.SourceLocationId,
                    reference: request.Reference);

                if (updateResult.IsError) return updateResult.Errors;

                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return mapper.Map<Result>(source: updateResult.Value);
            }
        }
    }
}