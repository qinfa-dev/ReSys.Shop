using MapsterMapper;

using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;
using ReSys.Core.Domain.Stores;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

using Serilog;

using Taxonomy = ReSys.Core.Domain.Catalog.Taxonomies.Taxonomy;

namespace ReSys.Core.Feature.Admin.Catalog.Taxonomies;

public static partial class TaxonomyModule
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
                var idRequired = CommonInput.Errors.Required(prefix: nameof(Taxonomy), nameof(Taxonomy.Id));
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

                Taxonomy? taxonomy = await unitOfWork.Context.Set<Taxonomy>()
                    .FindAsync(keyValues: [command.Id], cancellationToken: cancellationToken);
                if (taxonomy == null)
                {
                    return Taxonomy.Errors.NotFound(id: command.Id);
                }

                if (request.StoreId != taxonomy.StoreId && request.StoreId.HasValue)
                {
                    Store? store = await unitOfWork.Context.Set<Store>()
                        .FindAsync(keyValues: [request.StoreId], cancellationToken: cancellationToken);
                    if (store == null)
                    {
                        return Store.Errors.NotFound(id: request.StoreId.Value);
                    }
                }

                if (taxonomy.Name != request.Name)
                {
                    var uniqueNameCheck = await unitOfWork.Context.Set<Taxonomy>()
                        .Where(m => !request.StoreId.HasValue || m.StoreId == request.StoreId.Value)
                        .Where(predicate: m => m.Id != taxonomy.Id)
                        .CheckNameIsUniqueAsync<Taxonomy, Guid>(
                            name: request.Name,
                            prefix: nameof(Taxonomy),
                            cancellationToken: cancellationToken);
                    if (uniqueNameCheck.IsError)
                        return uniqueNameCheck.Errors;
                }

                await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

                var updateResult = taxonomy.Update(
                    storeId: request.StoreId,
                    name: request.Name,
                    presentation: request.Presentation,
                    position: request.Position,
                    publicMetadata: request.PublicMetadata,
                    privateMetadata: request.PrivateMetadata);

                if (updateResult.IsError) return updateResult.Errors;
                unitOfWork.Context.Set<Taxonomy>().Update(entity: updateResult.Value);


                await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
                await unitOfWork.CommitTransactionAsync(cancellationToken: cancellationToken);

                return mapper.Map<Result>(source: updateResult.Value);

            }
        }

        public sealed class EventHandler(IUnitOfWork unitOfWork) : IDomainEventHandler<Taxonomy.Events.Updated>
        {
            public async Task Handle(Taxonomy.Events.Updated notification, CancellationToken cancellationToken)
            {

                var taxonomy = await unitOfWork.Context.Set<Taxonomy>()
                    .Include(m=>m.Taxons)
                    .FirstOrDefaultAsync(m => m.Id == notification.TaxonomyId, cancellationToken);
                if (taxonomy == null)
                {
                    Log.Information("Taxonomy with Id {TaxonomyId} not found for Updated event handling.", notification.TaxonomyId);
                    return;
                }

                var rootTaxon = taxonomy.Root;
                if (rootTaxon != null)
                {
                    var updateRootTaxon = rootTaxon.Update(
                        name: notification.Name,
                        presentation: notification.Presentation);

                    if (updateRootTaxon.IsError)
                    {
                        Log.Information("Failed to update root taxon for Taxonomy Id {TaxonomyId}: {Errors}", notification.TaxonomyId, updateRootTaxon.Errors);
                        return;
                    }
                    unitOfWork.Context.Set<Taxon>()
                        .Update(entity: updateRootTaxon.Value);
                    await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
                }
                else if (rootTaxon == null)
                {
                    var newRoot = Taxon.Create(
                        taxonomyId: notification.TaxonomyId,
                        name: taxonomy.Name,
                        parentId: null,
                        presentation: taxonomy.Presentation);

                    if (newRoot.IsError)
                    {
                        Log.Information("Failed to update root taxon for Taxonomy Id {TaxonomyId}: {Errors}", notification.TaxonomyId, newRoot.Errors);
                        return;
                    }
                    unitOfWork.Context.Set<Taxon>()
                        .Add(entity: newRoot.Value);
                    await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
                }
            }
        }
    }
}