using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Inventories.StockLocations;

public static partial class StockLocationModule
{
    public static class Restore
    {
        public sealed record Command(Guid Id) : ICommand<Success>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Id).NotEmpty();
            }
        }

        public sealed class CommandHandler(IUnitOfWork unitOfWork)
            : ICommandHandler<Command, Success>
        {
            public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken ct)
            {
                var location = await unitOfWork.Context.Set<StockLocation>()
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(predicate: m => m.Id == command.Id, cancellationToken: ct);

                if (location == null)
                    return StockLocation.Errors.NotFound(id: command.Id);

                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                var restoreResult = location.Restore();
                if (restoreResult.IsError) return restoreResult.Errors;

                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return Result.Success;
            }
        }
    }
}
