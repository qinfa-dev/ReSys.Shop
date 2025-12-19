using Microsoft.Extensions.Logging;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.Classifications;
using ReSys.Core.Domain.Catalog.Taxonomies;
using ReSys.Core.Domain.Catalog.Taxonomies.Rules;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Catalog.Taxons;

public static partial class TaxonModule
{
    public static class Services
    {
        public interface IRegeneration
        {
            Task RegenerateProductsForTaxonAsync(Guid taxonId, CancellationToken cancellationToken);
        }

        public interface IHierarchy
        {
            Task<ErrorOr<Success>> RebuildAsync(Guid taxonomyId, CancellationToken cancellationToken);
            Task<ErrorOr<Success>> ValidateHierarchyAsync(Guid taxonomyId, CancellationToken cancellationToken);
            Task<ErrorOr<Success>> RebuildNestedSetsAsync(Guid taxonomyId, CancellationToken cancellationToken);
            Task<ErrorOr<Success>> RegeneratePermalinksAsync(Guid taxonomyId, CancellationToken cancellationToken);
            Task<Models.TreeListItem> BuildTaxonTreeAsync(Models.HierarchyParameter param, CancellationToken cancellationToken);
            Task<PagedList<Models.FlatListItem>> GetFlatTaxonsAsync(Models.HierarchyParameter param, CancellationToken cancellationToken);
        }


        public sealed class HierarchyService(
            IUnitOfWork unitOfWork,
            ILogger<HierarchyService> logger) : IHierarchy
        {
            public async Task<ErrorOr<Success>> RebuildAsync(Guid taxonomyId, CancellationToken ct)
            {
                if (taxonomyId == Guid.Empty)
                    return Taxonomy.Errors.TaxonomyRequired;

                try
                {
                    var validateResult = await ValidateHierarchyAsync(taxonomyId: taxonomyId, ct: ct);
                    if (validateResult.IsError)
                        return validateResult.Errors;

                    var nestedSetResult = await RebuildNestedSetsAsync(taxonomyId: taxonomyId, ct: ct);
                    if (nestedSetResult.IsError)
                        return nestedSetResult.Errors;

                    var permalinkResult = await RegeneratePermalinksAsync(taxonomyId: taxonomyId, ct: ct);
                    if (permalinkResult.IsError)
                        return permalinkResult.Errors;

                    await unitOfWork.SaveChangesAsync(cancellationToken: ct);

                    logger.LogInformation(message: "Successfully rebuilt taxonomy {TaxonomyId}", args: taxonomyId);
                    return Result.Success;
                }
                catch (Exception ex)
                {
                    logger.LogError(exception: ex, message: "Failed to rebuild taxonomy {TaxonomyId}", args: taxonomyId);
                    return Error.Failure(code: "TaxonHierarchy.RebuildFailed", description: "Failed to rebuild taxonomy hierarchy.");
                }
            }

            public async Task<ErrorOr<Success>> ValidateHierarchyAsync(Guid taxonomyId, CancellationToken ct)
            {
                if (taxonomyId == Guid.Empty)
                    return Taxonomy.Errors.TaxonomyRequired;

                var allTaxons = await unitOfWork.Context.Set<Taxon>()
                    .Where(predicate: t => t.TaxonomyId == taxonomyId)
                    .ToListAsync(cancellationToken: ct);

                if (allTaxons.Count == 0)
                    return Result.Success;

                var taxonDict = allTaxons.ToDictionary(keySelector: t => t.Id);

                var cycleResult = DetectCycle(taxonDict: taxonDict);
                if (cycleResult.IsError)
                {
                    logger.LogError(message: "Cycle or invalid parent detected in taxonomy {TaxonomyId}: {Errors}", args: [taxonomyId, string.Join(separator: ", ", values: cycleResult.Errors.Select(selector: e => e.Description))]);
                    return cycleResult.Errors;
                }

                var roots = allTaxons.Where(predicate: t => !t.ParentId.HasValue).ToList();
                if (roots.Count > 1)
                {
                    logger.LogError(message: "Taxonomy {TaxonomyId} has {RootCount} root taxons", args: [taxonomyId, roots.Count]);
                    return Taxon.Errors.RootConflict;
                }

                if (roots.Count == 0)
                {
                    logger.LogError(message: "No root taxon found for taxonomy {TaxonomyId}", args: taxonomyId);
                    return Error.NotFound(code: "NoRootTaxon", description: $"No root taxon found for taxonomy {taxonomyId}.");
                }

                logger.LogInformation(message: "Hierarchy validation passed for taxonomy {TaxonomyId}", args: taxonomyId);
                return Result.Success;
            }

            public async Task<ErrorOr<Success>> RebuildNestedSetsAsync(Guid taxonomyId, CancellationToken ct)
            {
                if (taxonomyId == Guid.Empty)
                    return Taxonomy.Errors.TaxonomyRequired;

                var allTaxons = await unitOfWork.Context.Set<Taxon>()
                    .Where(predicate: t => t.TaxonomyId == taxonomyId)
                    .ToListAsync(cancellationToken: ct);

                if (allTaxons.Count == 0)
                    return Result.Success;

                var taxonDict = allTaxons.ToDictionary(keySelector: t => t.Id);

                allTaxons.ForEach(action: t =>
                {
                    t.Children = new List<Taxon>();
                    t.Parent = null;
                });

                foreach (var taxon in allTaxons.Where(predicate: t => t.ParentId.HasValue))
                {
                    var parent = taxonDict[key: taxon.ParentId!.Value];
                    parent.Children.Add(item: taxon);
                    taxon.Parent = parent;
                }

                var root = allTaxons.FirstOrDefault(predicate: t => t.Parent == null);
                if (root == null)
                {
                    logger.LogError(message: "No root taxon found for taxonomy {TaxonomyId}", args: taxonomyId);
                    return Error.NotFound(code: "NoRootTaxon", description: $"No root taxon found for taxonomy {taxonomyId}.");
                }

                var result = ComputeNestedSets(taxon: root, depth: 0, counter: 1);
                if (result.IsError)
                    return result.Errors;

                logger.LogInformation(message: "Nested sets rebuilt for taxonomy {TaxonomyId}", args: taxonomyId);
                return Result.Success;
            }

            public async Task<ErrorOr<Success>> RegeneratePermalinksAsync(Guid taxonomyId, CancellationToken ct)
            {
                if (taxonomyId == Guid.Empty)
                    return Taxonomy.Errors.TaxonomyRequired;

                var allTaxons = await unitOfWork.Context.Set<Taxon>()
                    .Where(predicate: t => t.TaxonomyId == taxonomyId)
                    .OrderBy(keySelector: t => t.Lft)
                    .ToListAsync(cancellationToken: ct);

                if (allTaxons.Count == 0)
                    return Result.Success;

                var taxonDict = allTaxons.ToDictionary(keySelector: t => t.Id);

                foreach (var taxon in allTaxons)
                {
                    string? parentPermalink = null;
                    string? parentPrettyName = null;

                    if (taxon.ParentId.HasValue && taxonDict.TryGetValue(key: taxon.ParentId.Value, value: out var parent))
                    {
                        parentPermalink = parent.Permalink;
                        parentPrettyName = parent.PrettyName;
                    }

                    var permalinkResult = taxon.RegeneratePermalinkAndPrettyName(parentPermalink: parentPermalink, parentPrettyName: parentPrettyName);
                    if (permalinkResult.IsError)
                        return permalinkResult.Errors;
                }

                logger.LogInformation(message: "Permalinks regenerated for taxonomy {TaxonomyId}", args: taxonomyId);
                return Result.Success;
            }

            public async Task<Models.TreeListItem> BuildTaxonTreeAsync(Models.HierarchyParameter param, CancellationToken ct)
            {
                IQueryable<Taxon> query = unitOfWork.Context.Set<Taxon>().AsNoTracking()
                    .Include(navigationPropertyPath: t => t.TaxonImages)
                    .Include(navigationPropertyPath: t => t.Classifications);

                if (param.TaxonomyId != null && param.TaxonomyId.Length != 0)
                    query = query.Where(predicate: taxon => param.TaxonomyId.Contains(taxon.TaxonomyId));

                if (!param.IncludeHidden)
                    query = query.Where(predicate: t => !t.HideFromNav);

                List<Taxon> allTaxons = await query.ToListAsync(cancellationToken: ct);

                if (!allTaxons.Any())
                    return new Models.TreeListItem();

                if (param.IncludeLeavesOnly.GetValueOrDefault())
                {
                    HashSet<Guid> nonLeafIds = new HashSet<Guid>(collection: allTaxons.Where(predicate: t => t.ParentId.HasValue).Select(selector: t => t.ParentId!.Value));
                    List<Models.TreeNodeItem> leaves = allTaxons
                        .Where(predicate: t => !nonLeafIds.Contains(item: t.Id))
                        .Select(selector: MapToTreeNode)
                        .ToList();
                    return new Models.TreeListItem { Tree = leaves };
                }

                Dictionary<Guid, Taxon> taxonsById = allTaxons.ToDictionary(keySelector: t => t.Id);
                ILookup<Guid?, Taxon> childrenLookup = allTaxons.ToLookup(keySelector: t => t.ParentId);

                HashSet<Guid>? activePath = null;
                List<Models.TreeNodeItem> breadcrumbs = new();
                Models.TreeNodeItem? focusedNode = null;
                Taxon? focusedTaxon = null;

                if (param.FocusedTaxonId.HasValue && taxonsById.TryGetValue(key: param.FocusedTaxonId.Value, value: out focusedTaxon))
                {
                    focusedNode = MapToTreeNode(taxon: focusedTaxon);
                    (activePath, breadcrumbs) = BuildPathAndBreadcrumbs(focusedTaxon: focusedTaxon, taxonsById: taxonsById);
                }

                List<Taxon> rootNodes = allTaxons.Where(predicate: t => t.IsRoot).OrderBy(keySelector: t => t.Position).ToList();
                List<Models.TreeNodeItem> tree = rootNodes.Select(selector: root => BuildTreeNode(taxon: root, lookup: childrenLookup, activePath: activePath)).ToList();

                Models.TreeNodeItem? focusedSubtree = null;
                if (focusedTaxon != null)
                {
                    focusedSubtree = BuildTreeNode(taxon: focusedTaxon, lookup: childrenLookup, activePath: activePath);
                }

                logger.LogInformation(message: "Successfully built taxon tree with {Count} root nodes.", args: tree.Count);

                return new Models.TreeListItem
                {
                    Tree = tree,
                    Breadcrumbs = breadcrumbs,
                    FocusedNode = focusedNode,
                    FocusedSubtree = focusedSubtree
                };
            }

            public async Task<PagedList<Models.FlatListItem>> GetFlatTaxonsAsync(Models.HierarchyParameter param, CancellationToken ct)
            {
                IQueryable<Taxon> query = unitOfWork.Context.Set<Taxon>()
                    .AsNoTracking()
                    .Include(navigationPropertyPath: t => t.TaxonImages)
                    .Include(navigationPropertyPath: t => t.Classifications);

                if (param.TaxonomyId != null && param.TaxonomyId.Length > 0)
                    query = query.Where(predicate: t => param.TaxonomyId.Contains(t.TaxonomyId));

                if (!param.IncludeHidden)
                    query = query.Where(predicate: t => !t.HideFromNav);

                if (param.FocusedTaxonId.HasValue)
                {
                    var focused = await unitOfWork.Context.Set<Taxon>().AsNoTracking()
                        .FirstOrDefaultAsync(predicate: t => t.Id == param.FocusedTaxonId.Value, cancellationToken: ct);
                    if (focused != null)
                    {
                        query = query.Where(predicate: t => t.TaxonomyId == focused.TaxonomyId && t.Lft >= focused.Lft && t.Lft <= focused.Rgt);
                    }
                }

                if (param.IncludeLeavesOnly.GetValueOrDefault())
                {
                    query = query.Where(predicate: t => t.Rgt - t.Lft == 1);
                }

                if (param.MaxDepth.HasValue)
                {
                    query = query.Where(predicate: t => t.Depth <= param.MaxDepth.Value);
                }

                query = query.ApplySearch(searchParams: param)
                    .ApplyFilters(filterParams: param)
                    .ApplySort(sortParams: param);

                var pagedTaxons = await query.ToPagedListAsync(pagingParams: param, cancellationToken: ct);
                var flatItems = pagedTaxons.Items.Select(selector: t => MapToFlatListItem(taxon: t)).ToList();

                return new PagedList<Models.FlatListItem>(
                    items: flatItems, totalCount: pagedTaxons.TotalCount, pageNumber: pagedTaxons.PageNumber, pageSize: pagedTaxons.PageSize);
            }

            // Private Helper Methods
            private static ErrorOr<Success> DetectCycle(IReadOnlyDictionary<Guid, Taxon> taxonDict)
            {
                var visited = new HashSet<Guid>();
                var recStack = new HashSet<Guid>();
                foreach (var id in taxonDict.Keys)
                {
                    if (!visited.Contains(item: id))
                    {
                        var result = DFS(id: id, taxonDict: taxonDict, visited: visited, recStack: recStack);
                        if (result.IsError)
                            return result.Errors;
                    }
                }
                return Result.Success;
            }

            private static ErrorOr<Success> DFS(Guid id, IReadOnlyDictionary<Guid, Taxon> taxonDict, HashSet<Guid> visited, HashSet<Guid> recStack)
            {
                visited.Add(item: id);
                recStack.Add(item: id);
                var taxon = taxonDict[key: id];
                if (taxon.ParentId.HasValue)
                {
                    var parentId = taxon.ParentId.Value;
                    if (!taxonDict.ContainsKey(key: parentId))
                    {
                        return Error.Validation(code: "InvalidParentId", description: $"Taxon '{id}' references non-existent parent '{parentId}'.");
                    }
                    if (!visited.Contains(item: parentId))
                    {
                        var result = DFS(id: parentId, taxonDict: taxonDict, visited: visited, recStack: recStack);
                        if (result.IsError)
                            return result;
                    }
                    else if (recStack.Contains(item: parentId))
                    {
                        return Error.Validation(code: "CycleDetected", description: $"Cycle detected in taxon hierarchy involving '{id}' and '{parentId}'.");
                    }
                }
                recStack.Remove(item: id);
                return Result.Success;
            }

            private static ErrorOr<int> ComputeNestedSets(Taxon taxon, int depth, int counter)
            {
                var lft = counter++;

                var children = taxon.Children.OrderBy(keySelector: t => t.Position).ToList();
                foreach (var child in children)
                {
                    var childResult = ComputeNestedSets(taxon: child, depth: depth + 1, counter: counter);
                    if (childResult.IsError)
                        return childResult.Errors;
                    counter = childResult.Value;
                }

                var rgt = counter++;

                var nestedSetResult = taxon.UpdateNestedSet(lft: lft, rgt: rgt, depth: depth);
                if (nestedSetResult.IsError)
                    return nestedSetResult.Errors;

                return counter;
            }

            private static Models.TreeNodeItem BuildTreeNode(Taxon taxon, ILookup<Guid?, Taxon> lookup, HashSet<Guid>? activePath)
            {
                Models.TreeNodeItem node = MapToTreeNode(taxon: taxon);
                if (activePath != null)
                {
                    node.IsInActivePath = activePath.Contains(item: taxon.Id);
                    node.IsExpanded = node.IsInActivePath;
                }
                List<Taxon> children = lookup[key: taxon.Id].OrderBy(keySelector: t => t.Position).ToList();
                if (children.Any())
                {
                    node.Children = children.Select(selector: child => BuildTreeNode(taxon: child, lookup: lookup, activePath: activePath)).ToList();
                    node.HasChildren = true;
                    node.ChildCount = children.Count;
                    node.IsLeaf = false;
                }
                else
                {
                    node.Children = new List<Models.TreeNodeItem>();
                    node.HasChildren = false;
                    node.ChildCount = 0;
                    node.IsLeaf = true;
                }
                return node;
            }

            private static (HashSet<Guid> activePath, List<Models.TreeNodeItem> breadcrumbs) BuildPathAndBreadcrumbs(
                Taxon focusedTaxon, IReadOnlyDictionary<Guid, Taxon> taxonsById)
            {
                HashSet<Guid> path = new();
                List<Models.TreeNodeItem> breadcrumbs = new();
                Taxon? current = focusedTaxon;
                while (current != null)
                {
                    path.Add(item: current.Id);
                    breadcrumbs.Insert(index: 0, item: MapToTreeNode(taxon: current));
                    current = current.ParentId.HasValue && taxonsById.TryGetValue(key: current.ParentId.Value, value: out Taxon? parent) ? parent : null;
                }
                return (path, breadcrumbs);
            }

            private static Models.TreeNodeItem MapToTreeNode(Taxon taxon)
            {
                return new Models.TreeNodeItem
                {
                    Id = taxon.Id,
                    Name = taxon.Name,
                    Presentation = taxon.Presentation,
                    PrettyName = taxon.PrettyName,
                    Permalink = taxon.Permalink,
                    ParentId = taxon.ParentId,
                    SortOrder = taxon.Position,
                    Depth = taxon.Depth,
                    Lft = taxon.Lft,
                    Rgt = taxon.Rgt,
                    ProductCount = taxon.Classifications.Count,
                    ImageUrl = taxon.TaxonImages.FirstOrDefault(predicate: a => a.Type == "default")?.Url,
                    SquareImageUrl = taxon.TaxonImages.FirstOrDefault(predicate: a => a.Type == "square")?.Url,
                    IsRoot = taxon.IsRoot,
                    HasChildren = (taxon.Rgt - taxon.Lft > 1),
                    IsLeaf = (taxon.Rgt - taxon.Lft == 1),
                    ChildCount = 0,
                    Children = new List<Models.TreeNodeItem>()
                };
            }

            private static Models.FlatListItem MapToFlatListItem(Taxon taxon)
            {
                return new Models.FlatListItem
                {
                    Id = taxon.Id,
                    Name = taxon.Name,
                    Presentation = taxon.Presentation,
                    PrettyName = taxon.PrettyName,
                    Depth = taxon.Depth,
                    ProductCount = taxon.Classifications.Count,
                    HasChildren = (taxon.Rgt - taxon.Lft > 1),
                    IsExpanded = false,
                    ParentId = taxon.ParentId,
                };
            }
        }

        public sealed class RegenerationService(
            IUnitOfWork unitOfWork,
            ILogger<RegenerationService> logger) : IRegeneration
        {
            public async Task RegenerateProductsForTaxonAsync(Guid taxonId, CancellationToken cancellationToken = default)
            {
                logger.LogInformation(message: "Starting product classification regeneration for Taxon {TaxonId}.", args: taxonId);
                try
                {
                    // Load: currency by default store
                    var taxonomy = await unitOfWork.Context.Set<Taxonomy>()
                        .FirstOrDefaultAsync(predicate: t => t.Taxons.Any(tx => tx.Id == taxonId), cancellationToken: cancellationToken);

                    if (taxonomy == null)
                    {
                        logger.LogWarning(message: "Taxonomy {TaxonomyId} not found. Aborting product classification regeneration.",
                            args: taxonId);
                        return;
                    }

                    var taxon = await unitOfWork.Context.Set<Taxon>()
                        .Include(navigationPropertyPath: t => t.TaxonRules)
                        .Include(navigationPropertyPath: t => t.Classifications)
                        .ThenInclude(navigationPropertyPath: c => c.Product)
                        .AsSplitQuery()
                        .FirstOrDefaultAsync(predicate: t => t.Id == taxonId, cancellationToken: cancellationToken);

                    if (taxon == null)
                    {
                        logger.LogWarning(message: "Taxon {TaxonId} not found. Aborting product classification regeneration.", args: taxonId);
                        return;
                    }

                    if (!taxon.Automatic)
                    {
                        logger.LogInformation(
                            message: "Taxon {TaxonId} is manual. No automatic product classification regeneration needed.", args: taxonId);
                        await CleanupOrphanedClassificationsAsync(taxon: taxon, cancellationToken: cancellationToken);
                        return;
                    }

                    // Validate rules before processing
                    if (!taxon.TaxonRules.Any())
                    {
                        logger.LogInformation(message: "Taxon {TaxonId} has no rules. Clearing all classifications.", args: taxonId);
                        await RemoveAllClassificationsAsync(taxon: taxon, cancellationToken: cancellationToken);
                        return;
                    }

                    // Build and execute query to find matching products
                    var matchingProductIds = await FindMatchingProductsAsync(taxon: taxon, cancellationToken: cancellationToken);

                    // Sync classifications
                    await SyncClassificationsAsync(taxon: taxon, matchingProductIds: matchingProductIds, cancellationToken: cancellationToken);

                    logger.LogInformation(message: "Finished product classification regeneration for Taxon {TaxonId}. " +
                                                   "Added: {AddedCount}, Removed: {RemovedCount}", args: [taxon.Id, matchingProductIds.Count, taxon.Classifications.Count(predicate: c => !matchingProductIds.Contains(item: c.ProductId))]);
                }
                catch (Exception ex)
                {
                    logger.LogError(exception: ex, message: "An error occurred during product classification regeneration for Taxon {TaxonId}.",
                        args: taxonId);
                    throw; // Re-throw to allow proper error handling upstream
                }
            }

            private async Task CleanupOrphanedClassificationsAsync(Taxon taxon, CancellationToken cancellationToken)
            {
                var orphanedClassifications = new List<Classification>();

                foreach (var classification in taxon.Classifications.ToList())
                {
                    orphanedClassifications.Add(item: classification);
                    taxon.Classifications.Remove(item: classification);
                    logger.LogWarning(
                        message: "Removed classification for non-existent product {ProductId} from manual Taxon {TaxonId}.", args: [classification.ProductId, taxon.Id]);
                }

                if (orphanedClassifications.Any())
                {
                    unitOfWork.Context.Set<Classification>().RemoveRange(entities: orphanedClassifications);
                    await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
                }
            }

            private async Task RemoveAllClassificationsAsync(Taxon taxon, CancellationToken cancellationToken)
            {
                if (!taxon.Classifications.Any()) return;

                var productIds = taxon.Classifications.Select(selector: c => c.ProductId).ToHashSet();
                unitOfWork.Context.Set<Classification>().RemoveRange(entities: taxon.Classifications);
                taxon.Classifications.Clear();

                await MarkProductsForRegenerationAsync(productIds: productIds, cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
            }

            private async Task<HashSet<Guid>> FindMatchingProductsAsync(Taxon taxon,
                CancellationToken cancellationToken = default)
            {
                var baseQuery = unitOfWork.Context.Set<Product>()
                    .Where(predicate: p => p.Status == Product.ProductStatus.Active);

                // Separate rules into different categories
                var (simpleRules, collectionRules) = CategorizeRules(rules: taxon.TaxonRules.ToList());

                IQueryable<Product> matchingProductsQuery;

                if (taxon.RulesMatchPolicy == "all")
                {
                    matchingProductsQuery = BuildAllMatchPolicyQuery(baseQuery: baseQuery, simpleRules: simpleRules, collectionRules: collectionRules);
                }
                else if (taxon.RulesMatchPolicy == "any")
                {
                    matchingProductsQuery =
                        await BuildAnyMatchPolicyQueryAsync(baseQuery: baseQuery, simpleRules: simpleRules, collectionRules: collectionRules, cancellationToken: cancellationToken);
                }
                else
                {
                    logger.LogWarning(message: "Unknown rules match policy '{Policy}' for Taxon {TaxonId}. No products will match.", args: [taxon.RulesMatchPolicy, taxon.Id]);
                    return new HashSet<Guid>();
                }

                return (await matchingProductsQuery
                        .Select(selector: p => p.Id)
                        .ToListAsync(cancellationToken: cancellationToken))
                    .ToHashSet();
            }

            private (List<TaxonRule> simple, List<TaxonRule> collection) CategorizeRules(List<TaxonRule> rules)
            {
                var simpleRules = new List<TaxonRule>();
                var collectionRules = new List<TaxonRule>();

                foreach (var rule in rules)
                {
                    if (rule.CanConvertToQueryFilter())
                    {
                        simpleRules.Add(item: rule);
                    }
                    else
                    {
                        collectionRules.Add(item: rule);
                    }
                }

                return (simpleRules, collectionRules);
            }

            private IQueryable<Product> BuildAllMatchPolicyQuery(
                IQueryable<Product> baseQuery,
                List<TaxonRule> simpleRules,
                List<TaxonRule> collectionRules)
            {
                // Apply simple rules using QueryFilterBuilder
                if (simpleRules.Any())
                {
                    try
                    {
                        var builder = QueryFilterBuilder.Create()
                            .WithDefaultLogic(logicalOperator: FilterLogicalOperator.All);

                        foreach (var rule in simpleRules)
                        {
                            builder.Add(
                                field: rule.GetFieldName(),
                                @operator: rule.GetFilterOperator(),
                                value: rule.Value,
                                logicalOperator: FilterLogicalOperator.All
                            );
                        }

                        baseQuery = builder.ApplyTo(query: baseQuery);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(exception: ex, message: "Error applying simple rules to query. Rules will be skipped.");
                    }
                }

                // Apply collection rules manually
                foreach (var rule in collectionRules)
                {
                    baseQuery = ApplyCollectionRule(baseQuery: baseQuery, rule: rule);
                }

                return baseQuery;
            }

            private async Task<IQueryable<Product>> BuildAnyMatchPolicyQueryAsync(
                IQueryable<Product> baseQuery,
                List<TaxonRule> simpleRules,
                List<TaxonRule> collectionRules,
                CancellationToken cancellationToken)
            {
                if (!simpleRules.Any() && !collectionRules.Any())
                {
                    return baseQuery.Where(predicate: p => false);
                }

                // For "any" policy with mixed rule types, we need to combine results
                var productIds = new HashSet<Guid>();

                // Get products matching simple rules
                if (simpleRules.Any())
                {
                    try
                    {
                        var builder = QueryFilterBuilder.Create()
                            .WithDefaultLogic(logicalOperator: FilterLogicalOperator.Any);

                        foreach (var rule in simpleRules)
                        {
                            builder.Add(
                                field: rule.GetFieldName(),
                                @operator: rule.GetFilterOperator(),
                                value: rule.Value,
                                logicalOperator: FilterLogicalOperator.Any
                            );
                        }

                        var simpleMatchIds = await builder.ApplyTo(query: baseQuery)
                            .Select(selector: p => p.Id)
                            .ToListAsync(cancellationToken: cancellationToken);

                        productIds.UnionWith(other: simpleMatchIds);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(exception: ex, message: "Error applying simple rules to query. Rules will be skipped.");
                    }
                }

                // Get products matching collection rules
                foreach (var rule in collectionRules)
                {
                    var ruleQuery = ApplyCollectionRule(baseQuery: baseQuery, rule: rule);
                    var ruleMatchIds = await ruleQuery
                        .Select(selector: p => p.Id)
                        .ToListAsync(cancellationToken: cancellationToken);

                    productIds.UnionWith(other: ruleMatchIds);
                }

                return baseQuery.Where(predicate: p => productIds.Contains(p.Id));
            }

            private IQueryable<Product> ApplyCollectionRule(IQueryable<Product> baseQuery, TaxonRule rule)
            {
                return rule.Type switch
                {
                    "variant_price" => ApplyVariantPriceRule(baseQuery: baseQuery, rule: rule),
                    "variant_sku" => ApplyVariantSkuRule(baseQuery: baseQuery, rule: rule),
                    "classification_taxon" => ApplyClassificationTaxonRule(baseQuery: baseQuery, rule: rule),
                    _ => baseQuery.Where(predicate: p => false) // Unknown rule type
                };
            }

            private IQueryable<Product> ApplyVariantPriceRule(IQueryable<Product> baseQuery, TaxonRule rule)
            {
                // This is a simplified example - you may need to adjust based on your Variant model
                return rule.MatchPolicy switch
                {
                    "is_equal_to" when decimal.TryParse(s: rule.Value, result: out var price) =>
                        baseQuery.Where(predicate: p => p.Variants.Any(v => v.PriceIn(null) == price)),
                    "greater_than" when decimal.TryParse(s: rule.Value, result: out var price) =>
                        baseQuery.Where(predicate: p => p.Variants.Any(v => v.PriceIn(null) > price)),
                    "less_than" when decimal.TryParse(s: rule.Value, result: out var price) =>
                        baseQuery.Where(predicate: p => p.Variants.Any(v => v.PriceIn(null) < price)),
                    _ => baseQuery.Where(predicate: p => false)
                };
            }

            private IQueryable<Product> ApplyVariantSkuRule(IQueryable<Product> baseQuery, TaxonRule rule)
            {
                return rule.MatchPolicy switch
                {
                    "is_equal_to" => baseQuery.Where(predicate: p => p.Variants.Any(v => v.Sku == rule.Value)),
                    "contains" => baseQuery.Where(predicate: p =>
                        p.Variants.Any(v => !string.IsNullOrEmpty(v.Sku) && v.Sku.Contains(rule.Value))),
                    "starts_with" => baseQuery.Where(predicate: p =>
                        p.Variants.Any(v => !string.IsNullOrEmpty(v.Sku) && v.Sku.StartsWith(rule.Value))),
                    _ => baseQuery.Where(predicate: p => false)
                };
            }

            private IQueryable<Product> ApplyClassificationTaxonRule(IQueryable<Product> baseQuery, TaxonRule rule)
            {
                if (!Guid.TryParse(input: rule.Value, result: out var taxonId))
                {
                    return baseQuery.Where(predicate: p => false);
                }

                return rule.MatchPolicy switch
                {
                    "is_equal_to" => baseQuery.Where(predicate: p => p.Classifications.Any(c => c.TaxonId == taxonId)),
                    "is_not_equal_to" => baseQuery.Where(predicate: p => p.Classifications.All(c => c.TaxonId != taxonId)),
                    _ => baseQuery.Where(predicate: p => false)
                };
            }

            private async Task SyncClassificationsAsync(Taxon taxon, HashSet<Guid> matchingProductIds,
                CancellationToken cancellationToken)
            {
                var existingClassifiedProductIds = taxon.Classifications.Select(selector: c => c.ProductId).ToHashSet();

                var productIdsToAdd = matchingProductIds.Except(second: existingClassifiedProductIds).ToList();
                var productIdsToRemove = existingClassifiedProductIds.Except(second: matchingProductIds).ToList();

                var productsToMarkForRegeneration = new HashSet<Guid>();

                // Handle additions
                foreach (var productId in productIdsToAdd)
                {
                    var classificationResult = Classification.Create(productId: productId, taxonId: taxon.Id);
                    if (classificationResult.IsError)
                    {
                        logger.LogError(message: "Failed to create classification for product {ProductId} in taxon {TaxonId}: {Error}", args: [productId, taxon.Id, classificationResult.FirstError.Description]);
                        continue;
                    }

                    taxon.Classifications.Add(item: classificationResult.Value);
                    productsToMarkForRegeneration.Add(item: productId);
                    logger.LogDebug(message: "Added classification for product {ProductId} to taxon {TaxonId}", args: [productId, taxon.Id]);
                }

                // Handle removals
                var classificationsToRemove = taxon.Classifications
                    .Where(predicate: c => productIdsToRemove.Contains(item: c.ProductId))
                    .ToList();

                foreach (var classification in classificationsToRemove)
                {
                    taxon.Classifications.Remove(item: classification);
                    productsToMarkForRegeneration.Add(item: classification.ProductId);
                    logger.LogDebug(message: "Removed classification for product {ProductId} from taxon {TaxonId}", args: [classification.ProductId, taxon.Id]);
                }

                // Mark products for regeneration
                if (productsToMarkForRegeneration.Any())
                {
                    await MarkProductsForRegenerationAsync(productIds: productsToMarkForRegeneration, cancellationToken: cancellationToken);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
            }

            private async Task MarkProductsForRegenerationAsync(HashSet<Guid> productIds, CancellationToken cancellationToken)
            {
                if (!productIds.Any()) return;

                var productsToUpdate = await unitOfWork.Context.Set<Product>()
                    .Where(predicate: p => productIds.Contains(p.Id))
                    .ToListAsync(cancellationToken: cancellationToken);

                foreach (var product in productsToUpdate)
                {
                    product.MarkedForRegenerateTaxonProducts = true;
                }
            }
        }
    }
}