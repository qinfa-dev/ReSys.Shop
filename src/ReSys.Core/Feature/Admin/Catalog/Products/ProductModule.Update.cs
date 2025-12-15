using MapsterMapper;

using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Catalog.Products;

public static partial class ProductModule
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
                RuleFor(expression: x => x.Id).NotEmpty();
                RuleFor(expression: x => x.Request).SetValidator(validator: new Models.ParameterValidator());
            }
        }

        public sealed class CommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
            : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken ct)
            {
                var request = command.Request;
                var product = await unitOfWork.Context.Set<Product>()
                    .Include(p => p.Variants) // Eager load variants
                    .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken: ct);
                if (product == null)
                    return Product.Errors.NotFound(id: command.Id);

                if (product.Name != request.Name)
                {
                    var uniqueNameCheck = await unitOfWork.Context.Set<Product>()
                        .Where(predicate: m => m.Id != product.Id)
                        .CheckNameIsUniqueAsync<Product, Guid>(name: request.Name, prefix: nameof(Product), cancellationToken: ct);
                    if (uniqueNameCheck.IsError)
                        return uniqueNameCheck.Errors;
                }

                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                var updateResult = product.Update(
                    name: request.Name,
                    presentation: request.Presentation,
                    description: request.Description,
                    slug: request.Slug,
                    metaTitle: request.MetaTitle,
                    metaDescription: request.MetaDescription,
                    metaKeywords: request.MetaKeywords,
                    availableOn: request.AvailableOn,
                    makeActiveAt: request.MakeActiveAt,
                    discontinueOn: request.DiscontinueOn,
                    isDigital: request.IsDigital,
                    publicMetadata: request.PublicMetadata,
                    privateMetadata: request.PrivateMetadata);

                if (updateResult.IsError) return updateResult.Errors;

                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return mapper.Map<Result>(source: updateResult.Value);
            }
        }
    }
}