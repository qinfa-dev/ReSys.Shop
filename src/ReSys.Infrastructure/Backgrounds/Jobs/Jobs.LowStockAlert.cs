using Microsoft.Extensions.Logging;

using Quartz;

namespace ReSys.Infrastructure.Backgrounds.Jobs;

public sealed class LowStockItem
{
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinimumStock { get; set; }
}

[DisallowConcurrentExecution]
public sealed class LowStockAlertJob(
    ILogger<LowStockAlertJob> logger) : IJob
{
    public static readonly JobKey JobKey = new(name: nameof(LowStockAlertJob),
        group: "inventory");
    public static readonly TriggerKey TriggerKey = new(name: $"{nameof(LowStockAlertJob)}Trigger",
        group: "inventory");

    public const string CronExpression = "0 0 9 ? * MON"; // Every Monday at 9 AM
    internal const string Description = "Sends alerts for products with stock below minimum threshold.";

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        logger.LogInformation(message: "Starting low stock alert job");

        try
        {
            List<LowStockItem> lowStockItems = await FindLowStockItems(cancellationToken: cancellationToken);
            if (lowStockItems.Count > 0)
            {
                await SendLowStockAlerts(items: lowStockItems,
                    cancellationToken: cancellationToken);
                logger.LogInformation(message: "Successfully sent low stock alerts for {Count} items",
                    args: lowStockItems.Count);
            }
            else
            {
                logger.LogInformation(message: "No low stock items found");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(exception: ex,
                message: "Error occurred during low stock alert processing");
            throw new JobExecutionException(cause: ex,
                refireImmediately: false);
        }
    }

    private static async Task<List<LowStockItem>> FindLowStockItems(CancellationToken cancellationToken)
    {
        await Task.Delay(millisecondsDelay: 100,
            cancellationToken: cancellationToken);
        return [];
    }

    private async Task SendLowStockAlerts(List<LowStockItem> items, CancellationToken cancellationToken)
    {
        await Task.Delay(millisecondsDelay: 100,
            cancellationToken: cancellationToken);
    }
}