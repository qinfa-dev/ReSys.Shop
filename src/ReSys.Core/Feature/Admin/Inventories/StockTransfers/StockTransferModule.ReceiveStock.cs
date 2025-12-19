using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.StockTransfers;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Inventories.StockTransfers;

public static partial class StockTransferModule
{
    // Receive Stock
    public static class ReceiveStock
    {
        public sealed record Request
        {
            public List<Models.VariantQuantity> Items { get; init; } = new();
        }

        public sealed class RequestValidator : AbstractValidator<Request>
        {
            public RequestValidator()
            {
                RuleFor(expression: x => x.Items)
                    .NotEmpty()
                    .WithMessage("At least one item is required.");

                RuleForEach(expression: x => x.Items)
                    .SetValidator(validator: new Models.VariantQuantityValidator());
            }
        }

        public sealed record Command(Guid Id, Request Request) : ICommand<Success>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Id).NotEmpty();
                RuleFor(expression: x => x.Request).SetValidator(validator: new RequestValidator());
            }
        }

        public sealed class CommandHandler(IUnitOfWork unitOfWork)
            : ICommandHandler<Command, Success>
        {
            public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken ct)
            {
                var transfer = await unitOfWork.Context.Set<StockTransfer>()
                    .Include(navigationPropertyPath: st => st.DestinationLocation)
                    .ThenInclude(navigationPropertyPath: sl => sl.StockItems)
                    .Include(navigationPropertyPath: st => st.Movements)
                    .FirstOrDefaultAsync(predicate: st => st.Id == command.Id, cancellationToken: ct);

                if (transfer == null)
                    return StockTransfer.Errors.NotFound(id: command.Id);

                // Load variants
                var variantIds = command.Request.Items.Select(selector: i => i.VariantId).ToList();
                var variants = await unitOfWork.Context.Set<Variant>()
                    .Where(predicate: v => variantIds.Contains(v.Id))
                    .ToDictionaryAsync(keySelector: v => v.Id, cancellationToken: ct);

                // Validate all variants exist
                foreach (var item in command.Request.Items)
                {
                    if (!variants.ContainsKey(key: item.VariantId))
                        return StockTransfer.Errors.VariantNotFound(variantId: item.VariantId);
                }

                // Build variant-quantity dictionary
                var variantsByQuantity = command.Request.Items
                    .ToDictionary(
                        keySelector: i => variants[key: i.VariantId],
                        elementSelector: i => i.Quantity);

                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                var receiveResult = transfer.Receive(
                    destinationLocation: transfer.DestinationLocation,
                    variantsByQuantity: variantsByQuantity);

                if (receiveResult.IsError) return receiveResult.Errors;

                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return Result.Success;
            }
        }
    }
}