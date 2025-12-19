using MapsterMapper;

using ReSys.Core.Domain.Settings.ShippingMethods;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Settings.ShippingMethods;

public static partial class ShippingMethodModule
{
    public static class Create
    {
        public sealed record Request : Models.Parameter;
        public sealed record Result : Models.Detail;
        public sealed record Command(Request Request) : ICommand<Result>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
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
                var param = command.Request;
                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                var uniqueNameCheck = await unitOfWork.Context.Set<ShippingMethod>()
                    .CheckNameIsUniqueAsync<ShippingMethod, Guid>(name: param.Name, prefix: nameof(ShippingMethod), cancellationToken: ct);
                if (uniqueNameCheck.IsError)
                    return uniqueNameCheck.Errors;

                var createResult = ShippingMethod.Create(
                    name: param.Name,
                    presentation: param.Presentation,
                    type: param.Type,
                    baseCost: param.BaseCost,
                    description: param.Description,
                    active: param.Active,
                    estimatedDaysMin: param.EstimatedDaysMin,
                    estimatedDaysMax: param.EstimatedDaysMax,
                    position: param.Position,
                    publicMetadata: param.PublicMetadata,
                    privateMetadata: param.PrivateMetadata,
                    displayOn: param.DisplayOn);

                if (createResult.IsError) return createResult.Errors;

                var shippingMethod = createResult.Value;


                unitOfWork.Context.Set<ShippingMethod>().Add(entity: shippingMethod);
                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return mapper.Map<Result>(source: shippingMethod);
            }
        }
    }
}
