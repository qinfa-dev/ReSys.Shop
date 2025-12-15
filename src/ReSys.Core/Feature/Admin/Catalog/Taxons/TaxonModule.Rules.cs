using Mapster;

using MapsterMapper;

using Microsoft.Extensions.Logging;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Catalog.Taxonomies.Rules;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.Taxons;

public static partial class TaxonModule
{
    public static class Rules
    {
        public static class Get
        {
            public sealed class Request : QueryableParams;
            public sealed record Query(Guid TaxonId, Request Request) : IQuery<PagedList<Models.RuleItem>>;

            public sealed class QueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<QueryHandler> logger)
                : IQueryHandler<Query, PagedList<Models.RuleItem>>
            {
                public async Task<ErrorOr<PagedList<Models.RuleItem>>> Handle(Query request, CancellationToken ct)
                {
                    try
                    {
                        PagedList<Models.RuleItem> rules = await unitOfWork.Context.Set<TaxonRule>()
                            .Where(predicate: r => r.TaxonId == request.TaxonId)
                            .OrderBy(keySelector: r => r.CreatedAt)
                            .AsQueryable()
                            .AsNoTracking()
                            .ApplySearch(searchParams: request.Request)
                            .ApplyFilters(filterParams: request.Request)
                            .ApplySort(sortParams: request.Request)
                            .ProjectToType<Models.RuleItem>(config: mapper.Config)
                            .ToPagedListOrAllAsync(pagingParams: request.Request, cancellationToken: ct);

                        return rules;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(exception: ex, message: "Error retrieving rules for taxon {TaxonId}", args: request.TaxonId);
                        return Error.Failure(code: "TaxonRules.GetFailed", description: "Failed to retrieve taxon rules");
                    }
                }
            }
        }

        public static class Update
        {
            public sealed record Request
            {
                public List<Models.RuleParameter> Rules { get; init; } = [];
            }

            public sealed class RequestValidator : AbstractValidator<Request>
            {
                public RequestValidator()
                {
                    RuleForEach(expression: x => x.Rules)
                        .SetValidator(validator: new Models.RuleParameterValidator());
                }
            }

            public sealed record Result(Guid TaxonId, List<Models.RuleItem> Rules);
            public record Command(Guid TaxonId, Request Request) : ICommand<Result>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(expression: x => x.TaxonId).NotEmpty();
                    RuleFor(expression: x => x.Request).SetValidator(validator: new RequestValidator());
                }
            }

            public sealed class CommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<CommandHandler> logger)
                : ICommandHandler<Command, Result>
            {
                public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken ct)
                {
                    try
                    {
                        var taxon = await unitOfWork.Context.Set<Taxon>()
                            .Include(navigationPropertyPath: t => t.TaxonRules)
                            .FirstOrDefaultAsync(predicate: t => t.Id == request.TaxonId, cancellationToken: ct);

                        if (taxon == null)
                            return Taxon.Errors.NotFound(id: request.TaxonId);

                        var addedRules = new List<TaxonRule>();
                        var updatedRules = new List<TaxonRule>();
                        var removedRules = new List<TaxonRule>();

                        // Remove: rules that are not in the new set
                        var currentRules = taxon.TaxonRules.ToList();
                        foreach (var rule in currentRules)
                        {
                            var matchingRule = request.Request.Rules.FirstOrDefault(predicate: r =>
                                r.Type == rule.Type &&
                                r.MatchPolicy == rule.MatchPolicy &&
                                r.PropertyName == rule.PropertyName);

                            if (matchingRule == null)
                            {
                                var removeResult = taxon.RemoveRule(ruleId: rule.Id);
                                if (removeResult.IsError) return removeResult.Errors;
                                removedRules.Add(item: rule);
                            }
                        }

                        // Add or update rules
                        foreach (var ruleParam in request.Request.Rules)
                        {
                            var existingRule = taxon.TaxonRules.FirstOrDefault(predicate: r =>
                                r.Type == ruleParam.Type &&
                                r.MatchPolicy == ruleParam.MatchPolicy &&
                                r.PropertyName == ruleParam.PropertyName);

                            if (existingRule != null)
                            {
                                // Update existing rule if value is different
                                if (existingRule.Value != ruleParam.Value)
                                {
                                    var updateResult = existingRule.Update(type: ruleParam.Value);
                                    if (updateResult.IsError) return updateResult.Errors;
                                    updatedRules.Add(item: existingRule);
                                }
                            }
                            else
                            {
                                // Add new rule
                                var createResult = TaxonRule.Create(
                                    taxonId: request.TaxonId,
                                    type: ruleParam.Type,
                                    value: ruleParam.Value,
                                    matchPolicy: ruleParam.MatchPolicy,
                                    propertyName: ruleParam.PropertyName);

                                if (createResult.IsError) return createResult.Errors;

                                var rule = createResult.Value;
                                taxon.TaxonRules.Add(item: rule);
                                addedRules.Add(item: rule);
                            }
                        }

                        await unitOfWork.SaveChangesAsync(cancellationToken: ct);

                        // Publish event for rule changes
                        if (addedRules.Any() || updatedRules.Any() || removedRules.Any())
                        {
                            taxon.AddDomainEvent(domainEvent: new Taxon.Events.Updated(
                                TaxonId: taxon.Id,
                                TaxonomyId: taxon.TaxonomyId,
                                NameOrPresentationChanged: false));

                            taxon.AddDomainEvent(domainEvent: new Taxon.Events.RegenerateProducts(
                                TaxonId: taxon.Id));

                            taxon.MarkedForRegenerateTaxonProducts = true;
                        }

                        logger.LogInformation(
                            message: "Successfully batch updated rules for taxon {TaxonId} - Added: {AddedCount}, Updated: {UpdatedCount}, Removed: {RemovedCount}",
                            args: [request.TaxonId, addedRules.Count, updatedRules.Count, removedRules.Count]);

                        var resultRules = taxon.TaxonRules
                            .Select(selector: mapper.Map<Models.RuleItem>)
                            .ToList();

                        return new Result(TaxonId: request.TaxonId, Rules: resultRules);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(exception: ex, message: "Error batch updating rules for taxon {TaxonId}", args: request.TaxonId);
                        return Error.Failure(code: "TaxonRules.BatchUpdateFailed", description: "Failed to batch update taxon rules");
                    }
                }
            }
        }

        public static class RegenerateProducts
        {
            public sealed record Command(Guid TaxonId) : ICommand<Success>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(expression: x => x.TaxonId).NotEmpty();
                }
            }

            public sealed class CommandHandler(Services.IRegeneration regenerationService, ILogger<CommandHandler> logger)
                : ICommandHandler<Command, Success>
            {
                public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken ct)
                {
                    try
                    {
                        await regenerationService.RegenerateProductsForTaxonAsync(taxonId: command.TaxonId, cancellationToken: ct);
                        logger.LogInformation(message: "Product regeneration completed successfully for taxon {TaxonId}", args: command.TaxonId);
                        return Result.Success;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(exception: ex, message: "Failed to regenerate products for taxon {TaxonId}", args: command.TaxonId);
                        return Error.Failure(code: "ProductRegeneration.Failed", description: "Failed to regenerate products for taxon.");
                    }
                }
            }
        }
    }
}