using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Stores;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Stores;

public static partial class StoreModule
{
    public static partial class Products
    {
        public static class Add
        {
            public record Request(Guid ProductId, bool Visible = true, bool Featured = false);
            public record Command(Guid StoreId, Request Request) : ICommand<Success>;

            public sealed class Validator : AbstractValidator<Request>
            {
                public Validator()
                {
                    RuleFor(x => x.ProductId)
                        .NotEmpty()
                        .WithMessage("Product ID is required.");
                }
            }

            public sealed class CommandHandler(IUnitOfWork unitOfWork) : ICommandHandler<Command, Success>
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

                    var product = await _dbContext.Set<Product>()
                                                  .FirstOrDefaultAsync(p => p.Id == command.Request.ProductId, ct);
                    if (product is null)
                    {
                        await unitOfWork.RollbackTransactionAsync(ct);
                        return Product.Errors.NotFound(command.Request.ProductId);
                    }

                    var addResult = store.AddProduct(product, command.Request.Visible, command.Request.Featured);

                    if (addResult.IsError)
                    {
                        await unitOfWork.RollbackTransactionAsync(ct);
                        return addResult.FirstError;
                    }

                    await unitOfWork.SaveChangesAsync(ct);
                    await unitOfWork.CommitTransactionAsync(ct);

                    return Result.Success;
                }
            }
        }
    }
}