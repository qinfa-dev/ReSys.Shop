using MapsterMapper;

using ReSys.Core.Domain.Settings.ShippingMethods;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Settings.ShippingMethods;

public static partial class ShippingMethodModule
{
    public static class Activate
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
                var shippingMethod = await unitOfWork.Context.Set<ShippingMethod>()
                    .FindAsync(keyValues: [command.Id], cancellationToken: ct);
                if (shippingMethod == null)
                    return ShippingMethod.Errors.NotFound(id: command.Id);

                if (shippingMethod.Active) // Already active
                    return mapper.Map<Result>(source: shippingMethod);

                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                var updateResult = shippingMethod.Update(active: true);
                if (updateResult.IsError) return updateResult.Errors;

                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return mapper.Map<Result>(source: shippingMethod);
            }
        }
    }
}
