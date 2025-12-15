using MapsterMapper;

using ReSys.Core.Domain.Catalog.PropertyTypes;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Catalog.PropertyTypes;

public static partial class PropertyTypeModule
{
    public static class UpdateDisplayOn
    {
        public record Request : IHasDisplayOn
        {
            public DisplayOn DisplayOn { get; set; }
        }

        public sealed class RequestValidator : AbstractValidator<Request>
        {
            public RequestValidator()
            {
                this.AddDisplayOnRules(prefix: nameof(PropertyType));
            }
        }

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

                RuleFor(m => m.Request)
                    .SetValidator(new RequestValidator());
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

                await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
                var updateResult = property.Update(
                    displayOn: request.DisplayOn);

                if (updateResult.IsError) return updateResult.Errors;

                await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
                await unitOfWork.CommitTransactionAsync(cancellationToken: cancellationToken);

                return mapper.Map<Result>(source: updateResult.Value);
            }
        }
    }
}