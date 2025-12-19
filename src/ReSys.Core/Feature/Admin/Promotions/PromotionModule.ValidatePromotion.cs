using ReSys.Core.Domain.Promotions.Promotions;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Promotions;

public static partial class PromotionModule
{
    public static class Validate
    {
        public sealed record Query(Guid Id) : IQuery<Success>;

        public sealed class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(x => x.Id)
                    .NotEmpty().WithMessage("Promotion ID is required.")
                    .WithErrorCode("Promotion.Id.Required");
            }
        }
        public sealed class QueryHandler(IApplicationDbContext dbContext) : IQueryHandler<Query, Success>
        {
            public async Task<ErrorOr<Success>> Handle(Query query, CancellationToken ct)
            {
                var promotion = await dbContext.Set<Promotion>()
                    .FirstOrDefaultAsync(p => p.Id == query.Id, ct);

                if (promotion == null)
                    return Promotion.Errors.NotFound(query.Id);

                return promotion.Validate();
            }
        }
    }
}