using MapsterMapper;

using ReSys.Core.Domain.Catalog.OptionTypes;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Catalog.OptionValues;

public static partial class OptionValueModule
{
    public static class Create
    {
        public sealed record Request : Models.Parameter;
        public sealed record Result : Models.ListItem;

        public sealed record Command(Request Request) : ICommand<Result>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Request)
                    .SetValidator(validator: new Models.ParameterValidator());
            }
        }

        public class CommandHandler(IUnitOfWork unitOfWork, IMapper mapper) : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken cancellationToken)
            {
                var param = command.Request;
                await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

                // Check if OptionType exists
                OptionType? optionType = await unitOfWork.Context.Set<OptionType>()
                    .FindAsync(keyValues: [param.OptionTypeId], cancellationToken: cancellationToken);
                if (optionType == null)
                {
                    return OptionType.Errors.NotFound(id: param.OptionTypeId);
                }
                var uniqueNameCheck = await unitOfWork.Context.Set<OptionValue>()
                    .CheckNameIsUniqueAsync<OptionValue, Guid>(
                        name: param.Name,
                        prefix: nameof(OptionValue),
                        cancellationToken: cancellationToken);

                if (uniqueNameCheck.IsError)
                    return uniqueNameCheck.Errors;

                var createResult = OptionValue.Create(
                    optionTypeId: param.OptionTypeId,
                    name: param.Name,
                    presentation: param.Presentation,
                    position: param.Position,
                    publicMetadata: param.PublicMetadata,
                    privateMetadata: param.PrivateMetadata);

                if (createResult.IsError) return createResult.Errors;
                var optionValue = createResult.Value;

                unitOfWork.Context.Set<OptionValue>().Add(entity: optionValue);
                await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

                await unitOfWork.CommitTransactionAsync(cancellationToken: cancellationToken);

                return mapper.Map<Result>(source: optionValue);
            }
        }
    }
}