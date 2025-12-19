using ReSys.Core.Domain.Settings.ShippingMethods;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Settings.ShippingMethods;

public static partial class ShippingMethodModule
{
    public static class Delete
    {
        public sealed record Command(Guid Id) : ICommand<Deleted>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Id).NotEmpty();
            }
        }

        public sealed class CommandHandler(IUnitOfWork unitOfWork)
            : ICommandHandler<Command, Deleted>
        {
            public async Task<ErrorOr<Deleted>> Handle(Command command, CancellationToken ct)
            {
                var shippingMethod = await unitOfWork.Context.Set<ShippingMethod>()
                    .FirstOrDefaultAsync(predicate: sm => sm.Id == command.Id, cancellationToken: ct);

                if (shippingMethod == null)
                    return ShippingMethod.Errors.NotFound(id: command.Id);

                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                var deleteResult = shippingMethod.Delete(); // Assuming this performs a soft delete on the domain model
                if (deleteResult.IsError) return deleteResult.Errors;

                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return Result.Deleted;
            }
        }
    }
}
