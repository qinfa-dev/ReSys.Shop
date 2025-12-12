using ReSys.Core.Common.Models.Wrappers.PagedLists;

namespace ReSys.Core.Feature.Catalog.Taxons;

public static partial class TaxonModule
{
    public static class Hierarchy
    {
        // Tree
        public static class Tree
        {
            public sealed class Request : Models.HierarchyParameter;
            public sealed record Query(Request Request) : IQuery<Models.TreeListItem>;

            public sealed class QueryHandler(Services.IHierarchy hierarchyService)
                : IQueryHandler<Query, Models.TreeListItem>
            {
                public async Task<ErrorOr<Models.TreeListItem>> Handle(Query query, CancellationToken ct)
                {
                    var result = await hierarchyService.BuildTaxonTreeAsync(param: query.Request, cancellationToken: ct);
                    return result;
                }
            }
        }

        // Flat List
        public static class FlatList
        {
            public sealed class Request : Models.HierarchyParameter;
            public sealed record Query(Request Request) : IQuery<PagedList<Models.FlatListItem>>;

            public sealed class QueryHandler(Services.IHierarchy hierarchyService)
                : IQueryHandler<Query, PagedList<Models.FlatListItem>>
            {
                public async Task<ErrorOr<PagedList<Models.FlatListItem>>> Handle(Query query, CancellationToken ct)
                {
                    var result = await hierarchyService.GetFlatTaxonsAsync(param: query.Request, cancellationToken: ct);
                    return result;
                }
            }
        }

        public static class Rebuild
        {
            public sealed record Command(Guid TaxonomyId) : ICommand<Success>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(expression: x => x.TaxonomyId).NotEmpty();
                }
            }

            public sealed class CommandHandler(Services.IHierarchy hierarchyService)
                : ICommandHandler<Command, Success>
            {
                public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken ct)
                {
                    return await hierarchyService.RebuildAsync(taxonomyId: command.TaxonomyId, cancellationToken: ct);
                }
            }
        }
        public static class Validate
        {
            public sealed record Query(Guid TaxonomyId) : IQuery<Success>;

            public sealed class QueryValidator : AbstractValidator<Query>
            {
                public QueryValidator()
                {
                    RuleFor(expression: x => x.TaxonomyId).NotEmpty();
                }
            }

            public sealed class QueryHandler(Services.IHierarchy hierarchyService)
                : IQueryHandler<Query, Success>
            {
                public async Task<ErrorOr<Success>> Handle(Query query, CancellationToken ct)
                {
                    return await hierarchyService.ValidateHierarchyAsync(taxonomyId: query.TaxonomyId, cancellationToken: ct);
                }
            }
        }
    }
}