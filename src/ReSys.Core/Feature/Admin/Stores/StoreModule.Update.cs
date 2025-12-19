using MapsterMapper;

using ReSys.Core.Domain.Stores;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Stores;

public static partial class StoreModule
{
    public static class Update
    {
        public record Request : Models.Parameter;
        public record Result : Models.Detail;
        public sealed record Command(Guid Id, Request Request) : ICommand<Result>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Id).NotEmpty();
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
                var request = command.Request;
                var store = await unitOfWork.Context.Set<Store>()
                    .FindAsync(keyValues: [command.Id], cancellationToken: ct);
                if (store == null)
                    return Store.Errors.NotFound(id: command.Id);

                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                var updateResult = store.Update(
                    name: request.Name,
                    presentation: request.Presentation,
                    url: request.Url,
                    mailFromAddress: request.MailFromAddress,
                    customerSupportEmail: request.CustomerSupportEmail,
                    metaTitle: request.MetaTitle,
                    metaDescription: request.MetaDescription,
                    metaKeywords: request.MetaKeywords,
                    seoTitle: request.SeoTitle,
                    available: request.Available,
                    guestCheckoutAllowed: request.GuestCheckoutAllowed,
                    timezone: request.Timezone,
                    defaultLocale: request.DefaultLocale,
                    defaultCurrency: request.DefaultCurrency,
                    publicMetadata: request.PublicMetadata,
                    privateMetadata: request.PrivateMetadata);

                if (updateResult.IsError) return updateResult.Errors;

                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return mapper.Map<Result>(source: updateResult.Value);
            }
        }
    }
}