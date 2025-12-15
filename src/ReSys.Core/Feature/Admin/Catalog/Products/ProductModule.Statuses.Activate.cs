using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Catalog.Products;

public static partial class ProductModule
{
    public static partial class Statuses
    {
        public static class Activate
        {
            public sealed record Command(Guid Id) : ICommand<Success>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(expression: x => x.Id).NotEmpty();
                }
            }

            public sealed class CommandHandler(IUnitOfWork unitOfWork)
                : ICommandHandler<Command, Success>
            {
                public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken ct)
                {
                    var product = await unitOfWork.Context.Set<Product>()
                        .FindAsync(keyValues: [command.Id], cancellationToken: ct);

                    if (product == null)
                        return Product.Errors.NotFound(id: command.Id);

                    await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                    var activateResult = product.Activate();
                    if (activateResult.IsError) return activateResult.Errors;

                    await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                    await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                    return Result.Success;
                }
            }
        }
    }
}

