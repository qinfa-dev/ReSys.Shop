
using MapsterMapper;

using ReSys.Core.Domain.Catalog.OptionTypes;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.OptionTypes;

public static partial class OptionTypeModule
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

                var uniqueNameCheck = await unitOfWork.Context.Set<OptionType>()
                    .CheckNameIsUniqueAsync<OptionType, Guid>(
                        name: param.Name,
                        prefix: nameof(OptionType),
                        cancellationToken: cancellationToken);

                if (uniqueNameCheck.IsError)
                    return uniqueNameCheck.Errors;

                var createResult = OptionType.Create(
                    name: param.Name,
                    presentation: param.Presentation,
                    filterable: param.Filterable,
                    position: param.Position,
                    publicMetadata: param.PublicMetadata,
                    privateMetadata: param.PrivateMetadata);

                if (createResult.IsError) return createResult.Errors;
                var optionType = createResult.Value;

                unitOfWork.Context.Set<OptionType>().Add(entity: optionType);
                await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

                await unitOfWork.CommitTransactionAsync(cancellationToken: cancellationToken);

                return mapper.Map<Result>(source: optionType);
            }
        }
    }
}