using MapsterMapper;

using ReSys.Core.Domain.Catalog.OptionTypes;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Catalog.OptionValues;

public static partial class OptionValueModule
{
    public static class Update
    {
        public record Request : Models.Parameter;

        public record Result : Models.ListItem;

        public sealed record Command(Guid Id, Request Request) : ICommand<Result>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                var idRequired = CommonInput.Errors.Required(prefix: nameof(OptionValue), nameof(OptionValue.Id));
                RuleFor(expression: x => x.Id)
                    .NotEmpty()
                    .WithErrorCode(idRequired.Code)
                    .WithMessage(idRequired.Description);

                RuleFor(expression: x => x.Request)
                    .SetValidator(validator: new Models.ParameterValidator());
            }
        }

        public sealed class CommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper
        ) : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken cancellationToken)
            {
                var request = command.Request;
                OptionType? optionType = await unitOfWork.Context.Set<OptionType>()
                    .FindAsync(keyValues: [request.OptionTypeId], cancellationToken: cancellationToken);

                if (optionType == null)
                    return OptionType.Errors.NotFound(id: request.OptionTypeId);

                var optionValue = await unitOfWork.Context.Set<OptionValue>()
                    .FindAsync(keyValues: [command.Id], cancellationToken: cancellationToken);
                if (optionValue == null)
                {
                    return OptionValue.Errors.NotFound(id: command.Id);
                }

                if (optionValue.Name != request.Name)
                {
                    var uniqueNameCheck = await unitOfWork.Context.Set<OptionValue>()
                        .Where(predicate: m => m.Id != optionValue.Id)
                        .CheckNameIsUniqueAsync<OptionValue, Guid>(
                            name: request.Name,
                            prefix: nameof(OptionValue),
                            cancellationToken: cancellationToken, 
                            exclusions: optionValue.Id);
                    if (uniqueNameCheck.IsError)
                        return uniqueNameCheck.Errors;
                }

                await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
                var updateResult = optionValue.Update(
                    name: request.Name,
                    presentation: request.Presentation,
                    position: request.Position,
                    publicMetadata: request.PublicMetadata,
                    privateMetadata: request.PrivateMetadata);

                if (updateResult.IsError) return updateResult.Errors;

                await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
                await unitOfWork.CommitTransactionAsync(cancellationToken: cancellationToken);

                return mapper.Map<Result>(source: updateResult.Value);

            }
        }
    }
}