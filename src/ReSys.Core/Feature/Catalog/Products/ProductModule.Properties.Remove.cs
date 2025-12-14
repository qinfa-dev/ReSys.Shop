using ReSys.Core.Domain.Catalog.Products.PropertyTypes;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.Products;

public static partial class ProductModule
{
    public static partial class Properties
    {
        public static class Remove
        {
            public sealed record Command(Guid ProductId, Guid PropertyId) : ICommand<Success>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(x => x.ProductId).NotEmpty();
                    RuleFor(x => x.PropertyId).NotEmpty();
                }
            }

            public sealed class CommandHandler(IUnitOfWork unitOfWork)
                : ICommandHandler<Command, Success>
            {
                public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken ct)
                {
                    var prop = await unitOfWork.Context.Set<ProductPropertyType>()
                        .FirstOrDefaultAsync(
                            p => p.ProductId == command.ProductId &&
                                 p.PropertyId == command.PropertyId,
                            ct);

                    if (prop == null)
                        return Result.Success;

                    unitOfWork.Context.Set<ProductPropertyType>().Remove(prop);
                    await unitOfWork.SaveChangesAsync(ct);

                    return Result.Success;
                }
            }
        }
    }
}