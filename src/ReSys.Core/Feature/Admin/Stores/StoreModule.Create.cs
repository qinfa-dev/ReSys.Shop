using MapsterMapper;

using ReSys.Core.Domain.Stores;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Stores;

public static partial class StoreModule
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

                var createResult = Store.Create(
                    name: param.Name,
                    presentation: param.Presentation,
                    code: param.Code,
                    url: param.Url,
                    currency: param.DefaultCurrency,
                    locale: param.DefaultLocale,
                    timezone: param.Timezone,
                    mailFromAddress: param.MailFromAddress,
                    customerSupportEmail: param.CustomerSupportEmail,
                    isDefault: param.Default,
                    publicMetadata: param.PublicMetadata,
                    privateMetadata: param.PrivateMetadata);

                if (createResult.IsError) return createResult.Errors;

                var store = createResult.Value;

                unitOfWork.Context.Set<Store>().Add(entity: store);
                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return mapper.Map<Result>(source: store);
            }
        }
    }
}