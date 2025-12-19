using MapsterMapper;

using ReSys.Core.Domain.Settings.PaymentMethods;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Settings.PaymentMethods;

public static partial class PaymentMethodModule
{
    public static class Restore
    {
        public sealed record Command(Guid Id) : ICommand<Result>;
        public sealed record Result : Models.Detail;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Id).NotEmpty();
            }
        }

        public sealed class CommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
            : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken ct)
            {
                var paymentMethod = await unitOfWork.Context.Set<PaymentMethod>()
                    .FindAsync(keyValues: [command.Id], cancellationToken: ct);
                if (paymentMethod == null)
                    return PaymentMethod.Errors.NotFound(id: command.Id);

                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                var restoreResult = paymentMethod.Restore();
                if (restoreResult.IsError) return restoreResult.Errors;

                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return mapper.Map<Result>(source: restoreResult.Value);
            }
        }
    }
}