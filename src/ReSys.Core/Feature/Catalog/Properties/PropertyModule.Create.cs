using MapsterMapper;

using ReSys.Core.Domain.Catalog.Properties;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.Properties;

public static partial class PropertyModule
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

                var uniqueNameCheck = await unitOfWork.Context.Set<Property>()
                    .CheckNameIsUniqueAsync<Property, Guid>(
                        name: param.Name,
                        prefix: nameof(Property),
                        cancellationToken: cancellationToken);

                if (uniqueNameCheck.IsError)
                    return uniqueNameCheck.Errors;

                var createResult = Property.Create(
                    name: param.Name,
                    presentation: param.Presentation,
                    kind: param.Kind,
                    filterable: param.Filterable,
                    displayOn: param.DisplayOn,
                    position: param.Position,
                    filterParam: param.FilterParam,
                    publicMetadata: param.PublicMetadata,
                    privateMetadata: param.PrivateMetadata);

                if (createResult.IsError) return createResult.Errors;
                var property = createResult.Value;

                unitOfWork.Context.Set<Property>().Add(entity: property);
                await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

                await unitOfWork.CommitTransactionAsync(cancellationToken: cancellationToken);

                return mapper.Map<Result>(source: property);
            }
        }
    }
}