using Microsoft.Extensions.Logging;

using Quartz;

namespace ReSys.Infrastructure.Backgrounds.Jobs;

[DisallowConcurrentExecution]
public sealed class InventoryUpdateJob(
    ILogger<InventoryUpdateJob> logger) : IJob
{
    public static readonly JobKey JobKey = new(name: nameof(InventoryUpdateJob),
        group: "inventory");
    public static readonly TriggerKey TriggerKey = new(name: $"{nameof(InventoryUpdateJob)}Trigger",
        group: "inventory");

    public const string CronExpression = "0 0 * * * ?"; // Every hour at minute 0
    internal const string Description = "Updates inventory levels and synchronizes with external systems.";

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        logger.LogInformation(message: "Starting inventory update job");

        try
        {
            int updatedCount = await UpdateInventoryLevels(cancellationToken: cancellationToken);
            logger.LogInformation(message: "Successfully updated inventory for {Count} products",
                args: updatedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(exception: ex,
                message: "Error occurred during inventory update");
            throw new JobExecutionException(cause: ex,
                refireImmediately: false);
        }
    }

    private static async Task<int> UpdateInventoryLevels(CancellationToken cancellationToken)
    {
        await Task.Delay(millisecondsDelay: 100,
            cancellationToken: cancellationToken);
        return 0;
    }
}