using ReSys.Core.Domain.Catalog.OptionTypes;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Catalog.OptionValues;

public static partial class OptionValueModule
{
    public static class Delete
    {
        public sealed record Command(Guid Id) : ICommand<Deleted>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                var idRequired = CommonInput.Errors.Required(prefix: nameof(OptionValue), nameof(OptionValue.Id));
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
                var optionValue = await unitOfWork.Context.Set<OptionValue>()
                    .FirstOrDefaultAsync(predicate: m => m.Id == command.Id, cancellationToken: cancellationToken);

                // Check: existence
                if (optionValue == null)
                    return OptionValue.Errors.NotFound(id: command.Id);

                // Check: deletable
                var deleteResult = optionValue.Delete();
                if (deleteResult.IsError) return deleteResult.Errors;

                await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
                unitOfWork.Context.Set<OptionValue>().Remove(entity: optionValue);
                await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
                await unitOfWork.CommitTransactionAsync(cancellationToken: cancellationToken);

                return Result.Deleted;
            }
        }
    }
}