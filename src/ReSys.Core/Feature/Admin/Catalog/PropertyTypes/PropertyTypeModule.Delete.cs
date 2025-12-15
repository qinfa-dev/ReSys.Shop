using ReSys.Core.Domain.Catalog.PropertyTypes;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.PropertyTypes;

public static partial class PropertyTypeModule
{
    public static class Delete
    {
        public sealed record Command(Guid Id) : ICommand<Deleted>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                var idRequired = CommonInput.Errors.Required(prefix: nameof(PropertyType), nameof(PropertyType.Id));
                RuleFor(expression: x => x.Id)
                    .NotEmpty()
                    .WithErrorCode(idRequired.Code)
                    .WithMessage(idRequired.Description);
            }
        }

        public sealed class CommandHandler(
            IUnitOfWork unitOfWork
        ) : ICommandHandler<Command, Deleted>
        {
            public async Task<ErrorOr<Deleted>> Handle(Command command, CancellationToken cancellationToken)
            {
                // Fetch: 
                var property = await unitOfWork.Context.Set<PropertyType>()
                    .Include(navigationPropertyPath: p => p.ProductPropertyTypes)
                    .FirstOrDefaultAsync(predicate: m => m.Id == command.Id, cancellationToken: cancellationToken);

                // Check: existence
                if (property == null)
                    return PropertyType.Errors.NotFound(id: command.Id);

                // Check: deletable
                var deleteResult = property.Delete();
                if (deleteResult.IsError) return deleteResult.Errors;

                await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
                unitOfWork.Context.Set<PropertyType>().Remove(entity: property);
                await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
                await unitOfWork.CommitTransactionAsync(cancellationToken: cancellationToken);

                return Result.Deleted;
            }
        }
    }
}