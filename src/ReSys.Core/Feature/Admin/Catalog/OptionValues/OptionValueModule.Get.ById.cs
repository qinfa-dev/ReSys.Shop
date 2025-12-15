using Mapster;

using MapsterMapper;

using ReSys.Core.Domain.Catalog.OptionTypes;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Catalog.OptionValues;

public static partial class OptionValueModule
{
    public static partial class Get
    {
        public static class ById
        {
            public sealed record Result : Models.Detail;
            public sealed record Query(Guid Id) : IQuery<Result>;
            public sealed class QueryHandler(
                IApplicationDbContext dbContext,
                IMapper mapper
            ) : IQueryHandler<Query, Result>
            {
                public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
                {
                    var optionValue = await dbContext.Set<OptionValue>()
                        .Include(navigationPropertyPath: ov => ov.OptionType)
                        .ProjectToType<Result>(config: mapper.Config)
                        .FirstOrDefaultAsync(predicate: p => p.Id == request.Id, cancellationToken: cancellationToken);

                    if (optionValue == null)
                        return OptionValue.Errors.NotFound(id: request.Id);

                    return optionValue;
                }
            }
        }
    }
}