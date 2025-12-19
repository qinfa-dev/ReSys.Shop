using Mapster;

using MapsterMapper;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Settings.PaymentMethods;
using ReSys.Core.Feature.Common.Persistence.Interfaces;
using ReSys.Core.Feature.Common.Payments.Interfaces.Encryptor;

namespace ReSys.Core.Feature.Admin.Settings.PaymentMethods;

public static partial class PaymentMethodModule
{
    public static partial class Get
    {
        public static class PagedList
        {
            public sealed class Request : QueryableParams;
            public sealed record Result : Models.ListItem;
            public sealed record Query(Request Request) : IQuery<PagedList<Result>>;

            public sealed class QueryHandler(IApplicationDbContext dbContext, IMapper mapper)
                : IQueryHandler<Query, PagedList<Result>>
            {
                public async Task<ErrorOr<PagedList<Result>>> Handle(Query command, CancellationToken ct)
                {
                    var pagedResult = await dbContext.Set<PaymentMethod>()
                        .AsNoTracking()
                        .ApplySearch(searchParams: command.Request)
                        .ApplyFilters(filterParams: command.Request)
                        .ApplySort(sortParams: command.Request)
                        .ProjectToType<Result>(config: mapper.Config)
                        .ToPagedListAsync(pagingParams: command.Request, cancellationToken: ct);

                    return pagedResult;
                }
            }
        }

        public static class ById
        {
            public sealed record Result : Models.Detail;
            public sealed record Query(Guid Id) : IQuery<Result>;

            public sealed class QueryHandler(
                IApplicationDbContext dbContext,
                IMapper mapper,
                IPaymentCredentialEncryptor encryptor)
                : IQueryHandler<Query, Result>
            {
                public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken ct)
                {
                    var paymentMethod = await dbContext.Set<PaymentMethod>()
                        .FirstOrDefaultAsync(predicate: pm => pm.Id == request.Id, cancellationToken: ct);

                    if (paymentMethod == null)
                        return PaymentMethod.Errors.NotFound(id: request.Id);

                    var result = mapper.Map<Result>(source: paymentMethod);

                    // Decrypt private metadata for admin view after mapping
                    if (paymentMethod.PrivateMetadata != null)
                    {
                        result.PrivateMetadata = paymentMethod.PrivateMetadata
                            .ToDictionary(
                                keySelector: entry => entry.Key,
                                elementSelector: entry =>
                                    entry.Value != null ? (object?)encryptor.Decrypt(entry.Value.ToString()!) : null);
                    }

                    return result;
                }
            }
        }
    }
}