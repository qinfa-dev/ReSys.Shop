using ReSys.Core.Domain.Stores;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Stores;

public static partial class StoreModule
{
    public static partial class Products
    {
        public static class Remove
        {
            public record Command(Guid StoreId, Guid ProductId) : ICommand<Success>;

            public sealed class Validator : AbstractValidator<Command>
            {
                public Validator()
                {
                    RuleFor(x => x.StoreId)
                        .NotEmpty()
                        .WithMessage("Store ID is required.");
                    RuleFor(x => x.ProductId)
                        .NotEmpty()
                        .WithMessage("Product ID is required.");
                }
            }

            public sealed class Handler(IUnitOfWork unitOfWork) : ICommandHandler<Command, Success>
            {
                private readonly IApplicationDbContext _dbContext = unitOfWork.Context;

                public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken ct)
                {
                    await unitOfWork.BeginTransactionAsync(ct);

                    var store = await _dbContext.Set<Store>()
                        .Include(s => s.StoreProducts)
                        .FirstOrDefaultAsync(s => s.Id == command.StoreId, ct);

                    if (store is null)
                    {
                        await unitOfWork.RollbackTransactionAsync(ct);
                        return Store.Errors.NotFound(command.StoreId);
                    }

                    var removeResult = store.RemoveProduct(command.ProductId);

                    if (removeResult.IsError)
                    {
                        await unitOfWork.RollbackTransactionAsync(ct);
                        return removeResult.FirstError;
                    }

                    await unitOfWork.SaveChangesAsync(ct);
                    await unitOfWork.CommitTransactionAsync(ct);

                    return Result.Success;

                }
            }
        }
    }
}