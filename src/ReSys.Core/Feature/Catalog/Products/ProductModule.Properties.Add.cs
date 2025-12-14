using Mapster;

using ReSys.Core.Domain.Catalog.Products.PropertyTypes;
using ReSys.Core.Feature.Catalog.PropertyTypes;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.Products;

public static partial class ProductModule
{
    public static partial class Properties
    {
        public static class Add
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
                    var productProperty = await unitOfWork.Context.Set<ProductPropertyType>()
                        .FirstOrDefaultAsync(p =>
                                p.ProductId == command.ProductId &&
                                p.PropertyId == command.PropertyId,
                            ct);

                    if (productProperty == null)
                        return ProductPropertyType.Errors.NotFound(command.PropertyId);

                    var updateResult = productProperty.Update(
                        value: propertyValue.Value,
                        position: propertyValue.Position);

                    if (updateResult.IsError)
                        return updateResult.FirstError;

                    unitOfWork.Context.Set<ProductPropertyType>().Update(updateResult.Value);
                    await unitOfWork.SaveChangesAsync(ct);

                    return productProperty.Adapt<Result>();
                }
            }
        }
    }
}