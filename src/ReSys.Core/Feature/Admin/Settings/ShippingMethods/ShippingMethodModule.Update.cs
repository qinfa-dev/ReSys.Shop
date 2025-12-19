using MapsterMapper;

using ReSys.Core.Domain.Settings.ShippingMethods;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Settings.ShippingMethods;

public static partial class ShippingMethodModule
{
    public static class Update
    {
        // Update Request should inherit from Models.Parameter to match PaymentMethodModule pattern
        public sealed record Request : Models.Parameter;
        public sealed record Result : Models.Detail;
        public sealed record Command(Guid Id, Request Request) : ICommand<Result>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Id).NotEmpty();
                RuleFor(expression: x => x.Request)
                    .SetValidator(new Models.ParameterValidator()); // Use the common parameter validator
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
                var shippingMethod = await unitOfWork.Context.Set<ShippingMethod>()
                    .FindAsync(keyValues: [command.Id], cancellationToken: ct);
                if (shippingMethod == null)
                    return ShippingMethod.Errors.NotFound(id: command.Id);

                if (!string.IsNullOrWhiteSpace(request.Name) && request.Name.Trim() != shippingMethod.Name)
                {
                    var uniqueNameCheck = await unitOfWork.Context.Set<ShippingMethod>()
                        .Where(predicate: m => m.Id != shippingMethod.Id)
                        .CheckNameIsUniqueAsync<ShippingMethod, Guid>(name: request.Name, prefix: nameof(ShippingMethod), cancellationToken: ct);
                    if (uniqueNameCheck.IsError)
                        return uniqueNameCheck.Errors;
                }

                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                // Update the domain method call to include PublicMetadata and PrivateMetadata
                var updateResult = shippingMethod.Update(
                    name: request.Name,
                    presentation: request.Presentation,
                    description: request.Description,
                    baseCost: request.BaseCost,
                    active: request.Active, // Added active from Models.Parameter
                    estimatedDaysMin: request.EstimatedDaysMin,
                    estimatedDaysMax: request.EstimatedDaysMax,
                    maxWeight: request.MaxWeight, // MaxWeight is in Models.Parameter but not in previous Update.Request
                    position: request.Position,
                    publicMetadata: request.PublicMetadata,
                    privateMetadata: request.PrivateMetadata,
                    displayOn: request.DisplayOn); // Added DisplayOn from Models.Parameter

                if (updateResult.IsError) return updateResult.Errors;

                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return mapper.Map<Result>(source: shippingMethod);
            }
        }
    }
}
