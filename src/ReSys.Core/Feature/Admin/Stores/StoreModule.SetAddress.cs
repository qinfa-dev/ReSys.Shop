using MapsterMapper;

using ReSys.Core.Domain.Stores;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Stores;

public static partial class StoreModule
{
    public static class SetAddress
    {
        public record Request : Models.AddressParameter;
        public record Result : Models.Detail;
        public sealed record Command(Guid Id, Request Request) : ICommand<Result>;

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
                var store = await unitOfWork.Context.Set<Store>()
                    .FindAsync(keyValues: [command.Id], cancellationToken: ct);
                if (store == null)
                    return Store.Errors.NotFound(id: command.Id);

                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                var updateResult = store.SetAddress(
                    address1: command.Request.Address1,
                    address2: command.Request.Address2,
                    city: command.Request.City,
                    zipcode: command.Request.Zipcode,
                    phone: command.Request.Phone,
                    company: command.Request.Company,
                    countryId: command.Request.CountryId,
                    stateId: command.Request.StateId);

                if (updateResult.IsError) return updateResult.Errors;

                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return mapper.Map<Result>(source: updateResult.Value);
            }
        }
    }
}