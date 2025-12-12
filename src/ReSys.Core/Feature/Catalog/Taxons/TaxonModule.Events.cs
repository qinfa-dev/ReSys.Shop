using Microsoft.Extensions.Logging;

using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;

namespace ReSys.Core.Feature.Catalog.Taxons;

public static partial class TaxonModule
{
    public static class Events
    {
        public sealed class TaxonRegenerateProductsEventHandler(
            Services.IRegeneration regeneration,
            ILogger<TaxonRegenerateProductsEventHandler> logger)
            : IDomainEventHandler<Taxon.Events.RegenerateProducts>
        {
            public async Task Handle(Taxon.Events.RegenerateProducts notification, CancellationToken cancellationToken)
            {
                logger.LogInformation(message: "Handling RegenerateProducts event for Taxon {TaxonId}", args: notification.TaxonId);

                try
                {
                    await regeneration.RegenerateProductsForTaxonAsync(
                        taxonId: notification.TaxonId,
                        cancellationToken: cancellationToken);

                    logger.LogInformation(message: "Successfully regenerated products for Taxon {TaxonId}", args: notification.TaxonId);
                }
                catch (Exception ex)
                {
                    logger.LogError(exception: ex, message: "Failed to regenerate products for Taxon {TaxonId}", args: notification.TaxonId);
                }
            }
        }
    }
}