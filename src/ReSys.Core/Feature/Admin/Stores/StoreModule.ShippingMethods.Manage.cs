using Mapster;

using MapsterMapper;

using ReSys.Core.Domain.Settings.ShippingMethods;
using ReSys.Core.Domain.Stores;
using ReSys.Core.Domain.Stores.ShippingMethods;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Stores;

public static partial class StoreModule
{
    public static partial class ShippingMethods
    {
        public static class Manage
        {
            public sealed record Request(IEnumerable<Models.StoreShippingMethodParameter> Data);

            public sealed record Result(IReadOnlyList<Models.StoreShippingMethodListItem> Items);

            public sealed record Command(Guid StoreId, Request Request) : ICommand<Result>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(x => x.StoreId).NotEmpty();
                    RuleFor(x => x.Request.Data).NotNull();
                    RuleForEach(x => x.Request.Data)
                        .SetValidator(new Models.StorePaymentMethodParameterValidator());
                }
            }

            public sealed class CommandHandler(
                IUnitOfWork unitOfWork,
                IMapper mapper) : ICommandHandler<Command, Result>
            {
                public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken ct)
                {
                    var store = await unitOfWork.Context.Set<Store>()
                        .Include(s => s.StoreShippingMethods)
                        .FirstOrDefaultAsync(s => s.Id == command.StoreId, ct);

                    if (store == null)
                        return Store.Errors.NotFound(command.StoreId);

                    var desired = command.Request.Data
                        .GroupBy(x => x.ShippingMethodId)
                        .ToDictionary(g => g.Key, g => g.First());

                    var current = store.StoreShippingMethods
                        .ToDictionary(x => x.ShippingMethodId);

                    var desiredIds = desired.Keys.ToHashSet();
                    var currentIds = current.Keys.ToHashSet();

                    await unitOfWork.BeginTransactionAsync(ct);

                    try
                    {
                        // Remove missing
                        foreach (var id in currentIds.Except(desiredIds))
                            store.RemoveShippingMethod(id);

                        // Add or Update
                        foreach (var (shippingMethodId, item) in desired)
                        {
                            var shippingMethod = await unitOfWork.Context.Set<ShippingMethod>()
                                .FindAsync([shippingMethodId], ct);

                            if (shippingMethod == null) continue;

                            var exists = current.ContainsKey(shippingMethodId);
                            if (!exists)
                            {
                                store.AddShippingMethod(
                                    method: shippingMethod,
                                    available: item.Available,
                                    storeBaseCost: item.StoreBaseCost);
                            }
                            else
                            {
                                var ssm = current[shippingMethodId];
                                var needsUpdate = ssm.Available != item.Available ||
                                                  ssm.StoreBaseCost != item.StoreBaseCost;

                                if (needsUpdate)
                                {
                                    ssm.Update(
                                        available: item.Available,
                                        storeBaseCost: item.StoreBaseCost);
                                }
                            }
                        }

                        await unitOfWork.SaveChangesAsync(ct);
                        await unitOfWork.CommitTransactionAsync(ct);

                        var finalList = await unitOfWork.Context.Set<StoreShippingMethod>()
                            .Include(ssm => ssm.ShippingMethod)
                            .Include(ssm => ssm.Store)
                            .Where(ssm => ssm.StoreId == command.StoreId)
                            .ProjectToType<Models.StoreShippingMethodListItem>(mapper.Config)
                            .ToListAsync(ct);

                        return new Result(finalList);
                    }
                    catch (Exception ex)
                    {
                        await unitOfWork.RollbackTransactionAsync(ct);
                        return Error.Unexpected(ex.Message);
                    }
                }
            }
        }
    }
}
