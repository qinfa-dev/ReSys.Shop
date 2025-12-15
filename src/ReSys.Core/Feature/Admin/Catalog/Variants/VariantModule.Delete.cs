using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.Variants;

public static partial class VariantModule
{
    public static class Delete
    {
        public sealed record Command(Guid Id) : ICommand<Deleted>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Id).NotEmpty();
            }
        }

        public sealed class CommandHandler(IUnitOfWork unitOfWork)
            : ICommandHandler<Command, Deleted>
        {
            public async Task<ErrorOr<Deleted>> Handle(Command command, CancellationToken ct)
            {
                var variant = await unitOfWork.Context.Set<Variant>()
                    .Include(navigationPropertyPath: v => v.LineItems)
                    .ThenInclude(navigationPropertyPath: li => li.Order)
                    .FirstOrDefaultAsync(predicate: m => m.Id == command.Id, cancellationToken: ct);

                if (variant == null)
                    return Variant.Errors.NotFound(id: command.Id);

                var deleteResult = variant.Delete();
                if (deleteResult.IsError) return deleteResult.Errors;

                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);
                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return Result.Deleted;
            }
        }
    }
}