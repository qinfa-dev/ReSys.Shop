using Mapster;

using MapsterMapper;

using ReSys.Core.Domain.Settings.PaymentMethods;
using ReSys.Core.Domain.Stores;
using ReSys.Core.Domain.Stores.PaymentMethods;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Stores;

public static partial class StoreModule
{
    public static partial class PaymentMethods
    {
        public static class Manage
        {
            public sealed record Request(IEnumerable<Models.StorePaymentMethodParameter> Items);

            public sealed record Result : Models.StorePaymentMethodSelectItem;
            public sealed record Command(Guid StoreId, Request Request) : ICommand<List<Result>>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(x => x.StoreId).NotEmpty();
                    RuleFor(x => x.Request.Items).NotNull();
                    RuleForEach(x => x.Request.Items)
                        .SetValidator(new Models.StorePaymentMethodParameterValidator());
                }
            }

            public sealed class CommandHandler(
                IUnitOfWork unitOfWork,
                IMapper mapper) : ICommandHandler<Command, List<Result>>
            {
                public async Task<ErrorOr<List<Result>>> Handle(Command command, CancellationToken ct)
                {
                    var store = await unitOfWork.Context.Set<Store>()
                        .Include(s => s.StorePaymentMethods)
                        .FirstOrDefaultAsync(s => s.Id == command.StoreId, ct);

                    if (store == null)
                        return Store.Errors.NotFound(command.StoreId);

                    var desired = command.Request.Items
                        .GroupBy(x => x.PaymentMethodId)
                        .ToDictionary(g => g.Key, g => g.First().Available);

                    var current = store.StorePaymentMethods
                        .ToDictionary(x => x.PaymentMethodId, x => x.Available);

                    var desiredIds = desired.Keys.ToHashSet();
                    var currentIds = current.Keys.ToHashSet();

                    await unitOfWork.BeginTransactionAsync(ct);

                    try
                    {
                        // Remove missing
                        foreach (var id in currentIds.Except(desiredIds))
                            store.RemovePaymentMethod(id);

                        // Add or Update
                        foreach (var (id, available) in desired)
                        {
                            var pm = await unitOfWork.Context.Set<PaymentMethod>().FindAsync([id], ct);
                            if (pm == null) continue; // Skip invalid — or return error if you prefer

                            var exists = current.ContainsKey(id);
                            if (!exists)
                                store.AddPaymentMethod(pm, available);
                            else if (current[id] != available)
                            {
                                var spm = store.StorePaymentMethods.First(x => x.PaymentMethodId == id);
                                spm.Update(available);
                            }
                        }

                        await unitOfWork.SaveChangesAsync(ct);
                        await unitOfWork.CommitTransactionAsync(ct);

                        // Return the final applied list
                        var finalList = await unitOfWork.Context.Set<StorePaymentMethod>()
                            .Include(spm => spm.PaymentMethod)
                            .Include(spm => spm.Store)
                            .Where(spm => spm.StoreId == command.StoreId)
                            .ProjectToType<Result>(mapper.Config)
                            .ToListAsync(ct);

                        return finalList;
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