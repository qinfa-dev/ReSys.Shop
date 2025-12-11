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
    public static class Create
    {
        public sealed record Param : AddressModule.Model.Param;
        public sealed record Result : AddressModule.Model.ListItem;

        public record Command(string? UserId, Param Param) : ICommand<Result>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
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

                User? user = await unitOfWork.Context.Set<User>()
                    .Include(navigationPropertyPath: u => u.UserAddresses)
                    .FirstOrDefaultAsync(predicate: u => u.Id == userId,
                        cancellationToken: cancellationToken);

                if (user is null)
                    return User.Errors.NotFound(credential: userId);

                Country? country = await unitOfWork.Context.Set<Country>()
                    .FirstOrDefaultAsync(predicate: c => c.Id == request.Param.CountryId,
                        cancellationToken: cancellationToken);
                if (country is null)
                    return Country.Errors.NotFound(id: request.Param.CountryId);

                if (request.Param.StateId.HasValue)
                {
                    string? name = request.Param.StateName?.ToSlug();
                    State? state = await unitOfWork.Context.Set<State>()
                        .FirstOrDefaultAsync(predicate: s => s.Id == request.Param.StateId.Value || s.Name == name,
                            cancellationToken: cancellationToken);
                    if (state is null)
                        return State.Errors.NotFound(id: request.Param.StateId.Value);
                }

                // Create: the UserAddress entity
                ErrorOr<UserAddress> userAddressResult = UserAddress.Create(
                    firstName: request.Param.FirstName,
                    lastName: request.Param.LastName,
                    userId: userId,
                    countryId: request.Param.CountryId,
                    address1: request.Param.Address1,
                    city: request.Param.City,
                    zipcode: request.Param.Zipcode,
                    stateId: request.Param.StateId,
                    address2: request.Param.Address2,
                    phone: request.Param.Phone,
                    company: request.Param.Company,
                    label: request.Param.Label,
                    quickCheckout: request.Param.QuickCheckout,
                    isDefault: request.Param.IsDefault,
                    type: request.Param.Type);

                if (userAddressResult.IsError)
                {
                    return userAddressResult.FirstError;
                }

                UserAddress userAddress = userAddressResult.Value;
                user.AddAddress(userAddress: userAddress);
                await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

                return mapper.Map<Result>(source: userAddress);
            }
        }
    }
}
