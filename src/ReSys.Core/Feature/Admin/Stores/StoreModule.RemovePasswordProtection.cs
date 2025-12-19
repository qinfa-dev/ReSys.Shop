using ReSys.Core.Domain.Stores;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Stores;

public static partial class StoreModule
{
    public static class RemovePasswordProtection
    {
        public sealed record Command(Guid Id) : ICommand<Success>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Id).NotEmpty();
            }
        }

        public sealed class CommandHandler(
            IUnitOfWork unitOfWork)
            : ICommandHandler<Command, Success>
        {
            public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken ct)
            {
                var store = await unitOfWork.Context.Set<Store>()
                    .FindAsync(keyValues: [command.Id], cancellationToken: ct);
                if (store == null)
                    return Store.Errors.NotFound(id: command.Id);

                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                var removeResult = store.RemovePasswordProtection();
                if (removeResult.IsError) return removeResult.Errors;

                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return Result.Success;
            }
        }
    }
}