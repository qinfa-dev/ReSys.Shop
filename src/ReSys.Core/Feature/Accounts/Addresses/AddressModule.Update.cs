using MapsterMapper;

using MediatR;

using ReSys.Core.Common.Extensions;
using ReSys.Core.Domain.Identity.UserAddresses;
using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Domain.Location.Countries;
using ReSys.Core.Domain.Location.States;
using ReSys.Core.Feature.Common.Persistence.Interfaces;
using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;

namespace ReSys.Core.Feature.Accounts.Addresses;

public static partial class AddressModule
{
    public static class Update
    {
        public sealed record Param : Model.Param;
        public sealed record Result : Model.Detail;
        public record Command(Guid Id, string? UserId, Param Param) : ICommand<Result>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.UserId).NotEmpty();
                RuleFor(expression: m => m.Param)
                    .SetValidator(validator: new AddressModule.Model.ParamValidator());
            }
        }

        public class Handler(IUserContext userContext, IUnitOfWork unitOfWork, IMapper mapper) : IRequestHandler<Command, ErrorOr<Result>>
        {
            public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
            {
                if (!userContext.IsAuthenticated || string.IsNullOrEmpty(value: userContext.UserId))
                    return User.Errors.Unauthorized;
                string userId = userContext.UserId;

                var user = await unitOfWork.Context.Set<User>()
                    .Include(navigationPropertyPath: u => u.UserAddresses)
                            .ThenInclude(navigationPropertyPath: a => a.Country)
                    .Include(navigationPropertyPath: u => u.UserAddresses)
                        .ThenInclude(navigationPropertyPath: ua => ua.State)
                    .FirstOrDefaultAsync(predicate: u => u.Id == userId,
                        cancellationToken: cancellationToken);

                if (user is null)
                {
                    return User.Errors.NotFound(credential: userId);
                }

                var userAddress = user.UserAddresses.FirstOrDefault(predicate: ua => ua.Id == request.Id);
                if (userAddress is null)
                {
                    return UserAddress.Errors.NotFound(id: request.Id);
                }

                var country = await unitOfWork.Context.Set<Country>()
                    .FirstOrDefaultAsync(predicate: c => c.Id == request.Param.CountryId,
                        cancellationToken: cancellationToken);
                if (country is null)
                {
                    return Country.Errors.NotFound(id: request.Param.CountryId);
                }

                if (request.Param.StateId.HasValue)
                {
                    var name = request.Param.StateName?.ToSlug();
                    State? state = await unitOfWork.Context.Set<State>()
                        .FirstOrDefaultAsync(predicate: s => s.Id == request.Param.StateId.Value || s.Name == name,
                            cancellationToken: cancellationToken);
                    if (state is null)
                        return State.Errors.NotFound(id: request.Param.StateId.Value);
                }
                var userAddressUpdateResult = userAddress.Update(
                    firstName: request.Param.FirstName,
                    lastName: request.Param.LastName,
                    label: request.Param.Label,
                    quickCheckout: request.Param.QuickCheckout,
                    isDefault: request.Param.IsDefault,
                    type: request.Param.Type,
                    address1: request.Param.Address1,
                    address2: request.Param.Address2,
                    city: request.Param.City,
                    zipcode: request.Param.Zipcode,
                    countryId: request.Param.CountryId,
                    stateId: request.Param.StateId,
                    phone: request.Param.Phone,
                    company: request.Param.Company);

                if (userAddressUpdateResult.IsError)
                {
                    return userAddressUpdateResult.FirstError;
                }

                await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

                return mapper.Map<Result>(source: userAddress);
            }
        }
    }
}
