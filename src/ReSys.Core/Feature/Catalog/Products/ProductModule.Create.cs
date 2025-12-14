using MapsterMapper;

using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.Products;

public static partial class ProductModule
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

        public class CommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
            : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken ct)
            {
                var param = command.Request;
                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                var uniqueNameCheck = await unitOfWork.Context.Set<Product>()
                    .CheckNameIsUniqueAsync<Product, Guid>(name: param.Name, prefix: nameof(Product), cancellationToken: ct);
                if (uniqueNameCheck.IsError)
                    return uniqueNameCheck.Errors;

                var createResult = Product.Create(
                    name: param.Name,
                    description: param.Description,
                    slug: param.Slug,
                    metaTitle: param.MetaTitle,
                    metaDescription: param.MetaDescription,
                    metaKeywords: param.MetaKeywords,
                    availableOn: param.AvailableOn,
                    makeActiveAt: param.MakeActiveAt,
                    discontinueOn: param.DiscontinueOn,
                    isDigital: param.IsDigital,
                    publicMetadata: param.PublicMetadata,
                    privateMetadata: param.PrivateMetadata);

                if (createResult.IsError) return createResult.Errors;

                unitOfWork.Context.Set<Product>().Add(entity: createResult.Value);
                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return mapper.Map<Result>(source: createResult.Value);
            }
        }
    }
}