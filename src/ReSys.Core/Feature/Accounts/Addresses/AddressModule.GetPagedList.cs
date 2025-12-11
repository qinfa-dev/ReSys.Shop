using Mapster;

using MapsterMapper;

using MediatR;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Identity.UserAddresses;
using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Accounts.Addresses;

public static partial class AddressModule
{
    public static class GetPagedList
    {
        public sealed class Param : QueryableParams;

        public sealed record Result : Model.ListItem;
        public record Query(string? UserId, Param Param) : IRequest<ErrorOr<PagedList<Result>>>;

        public class Handler(IUnitOfWork unitOfWork, IMapper mapper) : IRequestHandler<Query, ErrorOr<PagedList<Result>>>
        {
            public async Task<ErrorOr<PagedList<Result>>> Handle(Query request, CancellationToken cancellationToken)
            {
                if (request.UserId == null)
                    return User.Errors.Unauthorized;

                var user = await unitOfWork.Context.Set<User>()
                    .FirstOrDefaultAsync(predicate: u => u.Id == request.UserId,
                        cancellationToken: cancellationToken);
                if (user is null)
                    return User.Errors.NotFound(credential: request.UserId);

                var query = unitOfWork.Context.Set<UserAddress>()
                    .Include(navigationPropertyPath: a => a.State);

                PagedList<Result> pagedResult = await query
                    .ApplySearch(searchParams: request.Param)
                    .ApplyFilters(filterParams: request.Param)
                    .ApplySort(sortParams: request.Param)
                    .ProjectToType<Result>(config: mapper.Config)
                    .ToPagedListAsync(pagingParams: request.Param,
                        cancellationToken: cancellationToken);

                return pagedResult;
            }
        }
    }
}
