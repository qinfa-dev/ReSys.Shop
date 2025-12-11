using MediatR;

using ReSys.Core.Domain.Identity.UserAddresses;
using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Accounts.Addresses;

public static partial class AddressModule
{
    public static class Delete
    {
        public record Command(Guid Id, string? UserId) : IRequest<ErrorOr<Deleted>>;
        public class Handler(IUnitOfWork unitOfWork) : IRequestHandler<Command, ErrorOr<Deleted>>
        {
            public async Task<ErrorOr<Deleted>> Handle(Command request, CancellationToken cancellationToken)
            {
                if (request.UserId == null)
                    return User.Errors.Unauthorized;

                var user = await unitOfWork.Context.Set<User>()
                    .Include(navigationPropertyPath: u => u.UserAddresses)
                    .FirstOrDefaultAsync(predicate: u => u.Id == request.UserId,
                        cancellationToken: cancellationToken);

                if (user is null)
                    return User.Errors.NotFound(credential: request.UserId);

                var userAddress = user.UserAddresses.FirstOrDefault(predicate: ua => ua.Id == request.Id);
                if (userAddress is null)
                {
                    return UserAddress.Errors.NotFound(id: request.Id);
                }

                var deleteResult = userAddress.Delete();
                if (deleteResult.IsError)
                {
                    return deleteResult.FirstError;
                }

                user.UserAddresses.Remove(item: userAddress);
                await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

                return Result.Deleted;
            }
        }
    }
}
