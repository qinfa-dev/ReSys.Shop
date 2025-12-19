using Microsoft.Extensions.Logging;

using ReSys.Core.Domain.Promotions.Rules;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Promotions;

public static partial class PromotionModule
{
    public static partial class Rules
    {
        public static partial class Taxons
        {
            public static class Manage
            {
                public sealed record Request
                {
                    public List<Guid> TaxonIds { get; set; } = new();
                }
                public sealed record Command(Guid PromotionId, Guid RuleId, Request Request) : ICommand<Success>;

                public sealed class CommandValidator : AbstractValidator<Command>
                {
                    public CommandValidator()
                    {
                        RuleFor(x => x.PromotionId)
                            .NotEmpty().WithMessage("Promotion ID is required.")
                            .WithErrorCode("Promotion.Id.Required");
                        RuleFor(x => x.RuleId)
                            .NotEmpty().WithMessage("Rule ID is required.")
                            .WithErrorCode("PromotionRule.Id.Required");
                        RuleFor(x => x.Request.TaxonIds)
                            .NotNull().WithMessage("Taxon IDs list cannot be null.")
                            .WithErrorCode("PromotionRule.Taxons.ListNull");
                        RuleForEach(x => x.Request.TaxonIds)
                            .NotEmpty().WithMessage("Taxon ID cannot be empty.")
                            .WithErrorCode("PromotionRule.TaxonId.Empty");
                    }
                }

                public sealed class CommandHandler(IUnitOfWork unitOfWork, ILogger<CommandHandler> logger)
                    : ICommandHandler<Command, Success>
                {
                    public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken ct)
                    {
                        var rule = await unitOfWork.Context.Set<PromotionRule>()
                            .Include(r => r.PromotionRuleTaxons)
                            .FirstOrDefaultAsync(r => r.Id == command.RuleId && r.PromotionId == command.PromotionId, ct);

                        if (rule == null)
                            return PromotionRule.Errors.NotFound(command.RuleId);

                        if (rule.Type != PromotionRule.RuleType.CategoryInclude && rule.Type != PromotionRule.RuleType.CategoryExclude)
                            return Error.Validation("PromotionRule.InvalidRuleType",
                                "Taxons can only be managed for CategoryInclude or CategoryExclude rules");

                        var existingTaxonIds = rule.PromotionRuleTaxons.Select(prt => prt.TaxonId).ToHashSet();
                        var desiredTaxonIds = command.Request.TaxonIds.ToHashSet();

                        // Taxons to remove
                        var taxonsToRemove = existingTaxonIds.Except(desiredTaxonIds).ToList();
                        foreach (var taxonId in taxonsToRemove)
                        {
                            var removeResult = rule.RemoveTaxon(taxonId);
                            if (removeResult.IsError)
                            {
                                logger.LogWarning("Failed to remove taxon {TaxonId} from rule {RuleId}: {Error}",
                                    taxonId, command.RuleId, removeResult.FirstError.Description);
                            }
                        }

                        // Taxons to add
                        var taxonsToAdd = desiredTaxonIds.Except(existingTaxonIds).ToList();
                        foreach (var taxonId in taxonsToAdd)
                        {
                            var addResult = rule.AddTaxon(taxonId);
                            if (addResult.IsError)
                            {
                                logger.LogWarning("Failed to add taxon {TaxonId} to rule {RuleId}: {Error}",
                                    taxonId, command.RuleId, addResult.FirstError.Description);
                            }
                        }

                        await unitOfWork.SaveChangesAsync(ct);
                        return Result.Success;
                    }
                }
            }
        }
    }
}