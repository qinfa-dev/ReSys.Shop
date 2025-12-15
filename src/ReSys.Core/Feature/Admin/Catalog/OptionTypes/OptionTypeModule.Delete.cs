using ReSys.Core.Domain.Catalog.OptionTypes;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.OptionTypes;

public static partial class OptionTypeModule
{
    public static class Delete
    {
        public sealed record Command(Guid Id) : ICommand<Deleted>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Id)
                    .NotEmpty()
                    .WithErrorCode(CommonInput.Errors.Required(prefix: nameof(OptionType), nameof(OptionType.Id)).Code)
                    .WithMessage(CommonInput.Errors.Required(prefix: nameof(OptionType), nameof(OptionType.Id)).Description);
            }
        }

        public sealed class CommandHandler(
            IUnitOfWork unitOfWork
        ) : ICommandHandler<Command, Deleted>
        {
            public async Task<ErrorOr<Deleted>> Handle(Command command, CancellationToken cancellationToken)
            {
                // Fetch: 
                var optionType = await unitOfWork.Context.Set<OptionType>()
                    .Include(navigationPropertyPath: ot => ot.OptionValues)
                    .FirstOrDefaultAsync(predicate: m => m.Id == command.Id, cancellationToken: cancellationToken);

                // Check: existence
                if (optionType == null)
                    return OptionType.Errors.NotFound(id: command.Id);

                // Check: deletable
                var deleteResult = optionType.Delete();
                if (deleteResult.IsError) return deleteResult.Errors;

                await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
                unitOfWork.Context.Set<OptionType>().Remove(entity: optionType);
                await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
                await unitOfWork.CommitTransactionAsync(cancellationToken: cancellationToken);

                return Result.Deleted;
            }
        }
    }
}