using MapsterMapper;

using ReSys.Core.Domain.Catalog.Products.Prices;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Catalog.Variants;

public static partial class VariantModule
{
    public static partial class Prices
    {
        public static class Set
        {
            public sealed record Request
            {
                public decimal? Amount { get; init; }
                public decimal? CompareAtAmount { get; init; }
                public string Currency { get; init; } = Price.Constraints.DefaultCurrency;
            }

            public sealed record Result : Models.PriceItem;
            public sealed record Command(Guid VariantId, Request Request) : ICommand<Result>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(expression: x => x.VariantId).NotEmpty();
                    RuleFor(expression: x => x.Request.Amount)
                        .GreaterThanOrEqualTo(valueToCompare: 0)
                        .When(predicate: x => x.Request.Amount.HasValue);
                    RuleFor(expression: x => x.Request.CompareAtAmount)
                        .GreaterThanOrEqualTo(valueToCompare: 0)
                        .When(predicate: x => x.Request.CompareAtAmount.HasValue);
                    RuleFor(expression: x => x.Request.Currency)
                        .NotEmpty()
                        .Length(exactLength: CommonInput.Constraints.CurrencyAndLanguage.CurrencyCodeLength)
                        .Must(predicate: x => Price.Constraints.ValidCurrencies.Contains(value: x))
                        .WithMessage(errorMessage: $"Currency must be one of: {string.Join(separator: ", ", value: Price.Constraints.ValidCurrencies)}.");
                }
            }

            public sealed class CommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
                : ICommandHandler<Command, Result>
            {
                public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken ct)
                {
                    var variant = await unitOfWork.Context.Set<Variant>()
                        .Include(navigationPropertyPath: v => v.Prices)
                        .FirstOrDefaultAsync(predicate: v => v.Id == command.VariantId, cancellationToken: ct);

                    if (variant == null)
                        return Variant.Errors.NotFound(id: command.VariantId);

                    await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                    var priceResult = variant.SetPrice(
                        amount: command.Request.Amount,
                        compareAtAmount: command.Request.CompareAtAmount,
                        currency: command.Request.Currency);

                    if (priceResult.IsError) return priceResult.Errors;

                    unitOfWork.Context.Set<Price>().Update(priceResult.Value);
                    await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                    await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                    return mapper.Map<Result>(source: priceResult.Value);
                }
            }
        }
    }
}