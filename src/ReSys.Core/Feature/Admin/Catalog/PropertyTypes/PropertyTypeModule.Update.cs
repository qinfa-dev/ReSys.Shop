using MapsterMapper;

using ReSys.Core.Domain.Catalog.PropertyTypes;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.PropertyTypes;

public static partial class PropertyTypeModule
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
                var idRequired = CommonInput.Errors.Required(prefix: nameof(PropertyType), nameof(PropertyType.Id));
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

                PropertyType? property = await unitOfWork.Context.Set<PropertyType>()
                    .FindAsync(keyValues: [command.Id], cancellationToken: cancellationToken);
                if (property == null)
                {
                    return PropertyType.Errors.NotFound(id: command.Id);
                }

                if (property.Name != request.Name)
                {
                    var uniqueNameCheck = await unitOfWork.Context.Set<PropertyType>()
                        .Where(predicate: m => m.Id != property.Id)
                        .CheckNameIsUniqueAsync<PropertyType, Guid>(
                            name: request.Name,
                            prefix: nameof(PropertyType),
                            cancellationToken: cancellationToken, 
                            exclusions: [property.Id]);
                    if (uniqueNameCheck.IsError)
                        return uniqueNameCheck.Errors;
                }

                await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
                var updateResult = property.Update(
                    name: request.Name,
                    presentation: request.Presentation,
                    kind: request.Kind,
                    filterable: request.Filterable,
                    displayOn: request.DisplayOn,
                    position: request.Position,
                    filterParam: request.FilterParam,
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