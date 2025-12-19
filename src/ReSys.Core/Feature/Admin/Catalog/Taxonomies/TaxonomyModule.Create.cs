using MapsterMapper;

using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Catalog.Taxonomies;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

using Serilog;

namespace ReSys.Core.Feature.Admin.Catalog.Taxonomies;

public static partial class TaxonomyModule
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

                var uniqueNameCheck = await unitOfWork.Context.Set<Taxonomy>()
                    .CheckNameIsUniqueAsync<Taxonomy, Guid>(
                        name: param.Name,
                        prefix: nameof(Taxonomy),
                        cancellationToken: cancellationToken);

                if (uniqueNameCheck.IsError)
                    return uniqueNameCheck.Errors;

                var createResult = Taxonomy.Create(
                    name: param.Name,
                    presentation: param.Presentation,
                    position: param.Position,
                    publicMetadata: param.PublicMetadata,
                    privateMetadata: param.PrivateMetadata);

                if (createResult.IsError) return createResult.Errors;
                var taxonomy = createResult.Value;

                unitOfWork.Context.Set<Taxonomy>().Add(entity: taxonomy);
                await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

                await unitOfWork.CommitTransactionAsync(cancellationToken: cancellationToken);

                return mapper.Map<Result>(source: taxonomy);
            }
        }
        public sealed class EventHandler(IUnitOfWork unitOfWork) : IDomainEventHandler<Taxonomy.Events.Created>
        {
            public async Task Handle(Taxonomy.Events.Created notification, CancellationToken cancellationToken)
            {

                var taxonomy = await unitOfWork.Context.Set<Taxonomy>()
                    .Include(m => m.Taxons)
                    .FirstOrDefaultAsync(m => m.Id == notification.TaxonomyId, cancellationToken);
                if (taxonomy == null)
                {
                    Log.Information("Taxonomy with Id {TaxonomyId} not found for Updated event handling.", notification.TaxonomyId);
                    return;
                }

                var taxonomyRoot = taxonomy.Root;
                if (taxonomyRoot == null)
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