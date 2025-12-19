using MapsterMapper;

using ReSys.Core.Domain.Settings.PaymentMethods;
using ReSys.Core.Feature.Common.Persistence.Interfaces;
using ReSys.Core.Feature.Common.Payments.Interfaces.Encryptor;

namespace ReSys.Core.Feature.Admin.Settings.PaymentMethods;

public static partial class PaymentMethodModule
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
            IMapper mapper,
            IPaymentCredentialEncryptor encryptor)
            : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken ct)
            {
                var param = command.Request;
                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                var uniqueNameCheck = await unitOfWork.Context.Set<PaymentMethod>()
                    .CheckNameIsUniqueAsync<PaymentMethod, Guid>(name: param.Name, prefix: nameof(PaymentMethod), cancellationToken: ct);
                if (uniqueNameCheck.IsError)
                    return uniqueNameCheck.Errors;

                var encryptedPrivateMetadata = param.PrivateMetadata?
                    .ToDictionary(
                        keySelector: entry => entry.Key,
                        elementSelector: entry =>
                            entry.Value != null ? (object?)encryptor.Encrypt(entry.Value.ToString()!) : null);

                var createResult = PaymentMethod.Create(
                    name: param.Name,
                    presentation: param.Presentation,
                    type: param.Type,
                    description: param.Description,
                    active: param.Active,
                    autoCapture: param.AutoCapture,
                    position: param.Position,
                    displayOn: param.DisplayOn,
                    publicMetadata: param.PublicMetadata,
                    privateMetadata: encryptedPrivateMetadata);

                if (createResult.IsError) return createResult.Errors;

                unitOfWork.Context.Set<PaymentMethod>().Add(entity: createResult.Value);
                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return mapper.Map<Result>(source: createResult.Value);
            }
        }
    }
}