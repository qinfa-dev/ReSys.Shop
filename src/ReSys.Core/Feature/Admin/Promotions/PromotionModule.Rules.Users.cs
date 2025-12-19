using MapsterMapper; // Re-adding this line

using Microsoft.Extensions.Logging; // Add this line

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Promotions.Rules;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Promotions;

public static partial class PromotionModule
{
    public static partial class Rules
    {
        public static class Users
        {
            public static class Get
            {
                public sealed class Request : QueryableParams;
                public sealed record Query(Guid PromotionId, Guid RuleId, Request Request) : IQuery<PagedList<Models.PromotionUsersRuleItem>>;

                public sealed class QueryHandler(IApplicationDbContext dbContext, IMapper mapper)
                    : IQueryHandler<Query, PagedList<Models.PromotionUsersRuleItem>>
                {
                    public async Task<ErrorOr<PagedList<Models.PromotionUsersRuleItem>>> Handle(Query request, CancellationToken ct)
                    {
                        // First, check if the rule exists and belongs to the promotion
                        var ruleExists = await dbContext.Set<PromotionRule>()
                            .AnyAsync(r => r.Id == request.RuleId && r.PromotionId == request.PromotionId, ct);

                        if (!ruleExists)
                            return PromotionRule.Errors.NotFound(request.RuleId);

                        var query = dbContext.Set<PromotionRuleUser>()
                            .Where(pru => pru.PromotionRuleId == request.RuleId)
                            .Include(pru => pru.User)
                            .AsNoTracking();

                        var pagedResult = await query
                            .ApplySearch(request.Request)
                            .ApplyFilters(request.Request)
                            .ApplySort(request.Request)
                            .Select(pru => mapper.Map<Models.PromotionUsersRuleItem>(pru.User))
                            .ToPagedListOrAllAsync(request.Request, ct);

                        return pagedResult;
                    }
                }
            }
            public static class Manage
            {
                public sealed record Request
                {
                    public List<string> UserIds { get; set; } = new();
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
                        RuleFor(x => x.Request.UserIds)
                            .NotNull().WithMessage("User IDs list cannot be null.")
                            .WithErrorCode("PromotionRule.Users.ListNull");
                        RuleForEach(x => x.Request.UserIds)
                            .NotEmpty().WithMessage("User ID cannot be empty.")
                            .WithErrorCode("PromotionRule.UserId.Empty");
                    }
                }

                public sealed class CommandHandler(IUnitOfWork unitOfWork, ILogger<CommandHandler> logger)
                    : ICommandHandler<Command, Success>
                {
                    public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken ct)
                    {
                        var rule = await unitOfWork.Context.Set<PromotionRule>()
                            .Include(r => r.PromotionRuleUsers)
                            .FirstOrDefaultAsync(r => r.Id == command.RuleId && r.PromotionId == command.PromotionId, ct);

                        if (rule == null)
                            return PromotionRule.Errors.NotFound(command.RuleId);

                        if (rule.Type != PromotionRule.RuleType.UserRole)
                            return Error.Validation("PromotionRule.InvalidRuleType",
                                "Users can only be managed for UserRole rules");

                        var existingUserIds = rule.PromotionRuleUsers.Select(pru => pru.UserId).ToHashSet();
                        var desiredUserIds = command.Request.UserIds.ToHashSet();

                        // Users to remove
                        var usersToRemove = existingUserIds.Except(desiredUserIds).ToList();
                        foreach (var userId in usersToRemove)
                        {
                            var removeResult = rule.RemoveUser(userId);
                            if (removeResult.IsError)
                            {
                                logger.LogWarning("Failed to remove user {UserId} from rule {RuleId}: {Error}",
                                    userId, command.RuleId, removeResult.FirstError.Description);
                            }
                        }

                        // Users to add
                        var usersToAdd = desiredUserIds.Except(existingUserIds).ToList();
                        foreach (var userId in usersToAdd)
                        {
                            var addResult = rule.AddUser(userId);
                            if (addResult.IsError)
                            {
                                logger.LogWarning("Failed to add user {UserId} to rule {RuleId}: {Error}",
                                    userId, command.RuleId, addResult.FirstError.Description);
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