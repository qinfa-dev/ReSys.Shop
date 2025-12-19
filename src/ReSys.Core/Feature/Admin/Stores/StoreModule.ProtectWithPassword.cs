using ReSys.Core.Domain.Stores;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Stores;

public static partial class StoreModule
{
    public static class ProtectWithPassword
    {
        public sealed record Request : Models.PasswordProtectionParameter;
        public sealed record Command(Guid Id, Request Request) : ICommand<Success>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Id).NotEmpty();
                RuleFor(expression: x => x.Request).SetValidator(validator: new Models.PasswordProtectionParameterValidator());
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

                // In a real application, hash the password here
                var hashedPassword = command.Request.Password; // Placeholder: Hash this!
                var protectResult = store.ProtectWithPassword(password: hashedPassword);
                if (protectResult.IsError) return protectResult.Errors;

                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return Result.Success;
            }
        }
    }
}