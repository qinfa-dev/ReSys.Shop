using MapsterMapper;

using MediatR;

using ReSys.Core.Domain.Identity.UserAddresses;
using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Accounts.Addresses;

public static partial class AddressModule
{
    public static class GetById
    {
        public sealed record Result : AddressModule.Model.Detail;
        public record Query(Guid Id, string? UserId) : ICommand<Result>;

        public class Handler(IUnitOfWork unitOfWork, IMapper mapper) : IRequestHandler<Query, ErrorOr<Result>>
        {
            public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
            {
                if (request.UserId == null)
                    return User.Errors.Unauthorized;

                var user = await unitOfWork.Context.Set<User>()
                    .FirstOrDefaultAsync(predicate: u => u.Id == request.UserId,
                        cancellationToken: cancellationToken);

                if (user is null)
                    return User.Errors.NotFound(credential: request.UserId);

                var userAddress = await unitOfWork.Context.Set<UserAddress>()
                    .Include(navigationPropertyPath: a => a.State)
                    .Include(navigationPropertyPath: a => a.Country)
                    .FirstOrDefaultAsync(predicate: ua => ua.Id == request.Id,
                        cancellationToken: cancellationToken);

                if (userAddress is null)
                    return UserAddress.Errors.NotFound(id: request.Id);

                return mapper.Map<Result>(source: userAddress);
            }
        }
    }
}
