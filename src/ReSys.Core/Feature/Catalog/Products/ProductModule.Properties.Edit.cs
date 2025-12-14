using Mapster;

using ReSys.Core.Domain.Catalog.Products.PropertyTypes;
using ReSys.Core.Domain.Catalog.PropertyTypes;
using ReSys.Core.Feature.Catalog.PropertyTypes;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.Products;

public static partial class ProductModule
{
    public static partial class Properties
    {
        public static class Edit
        {
            public record Request : Models.PropertyParameter;
            public sealed record Result : PropertyTypeModule.Models.SelectItem;
            public sealed record Command(
                Guid ProductId,
                Guid PropertyId,
                Request Request) : ICommand<Result>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(x => x.ProductId).NotEmpty();
                    RuleFor(x => x.PropertyId).NotEmpty();
                    RuleFor(x => x.Request)
                        .SetValidator(new Models.ParameterValidator());
                }
            }

            public sealed class CommandHandler(IUnitOfWork unitOfWork)
                : ICommandHandler<Command, Result>
            {
                public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken ct)
                {
                    var propertyValue = command.Request;
                    var propertyType = await unitOfWork.Context.Set<PropertyType>()
                        .FirstOrDefaultAsync(p =>
                                p.Id == command.PropertyId,
                            ct);

                    if (propertyType == null)
                        return PropertyType.Errors.NotFound(command.PropertyId);

                    var createResult = ProductPropertyType.Create(
                        productId: command.ProductId,
                        propertyId: command.PropertyId,
                        value: propertyValue.Value,
                        position: propertyValue.Position);

                    if (createResult.IsError)
                        return createResult.FirstError;

                    unitOfWork.Context.Set<ProductPropertyType>().Add(createResult.Value);
                    await unitOfWork.SaveChangesAsync(ct);

                    return propertyType.Adapt<Result>();
                }
            }
        }
    }
}