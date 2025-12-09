using Microsoft.Extensions.Logging;

using Quartz;

namespace ReSys.Infrastructure.Backgrounds.Jobs;

public sealed class SalesReportData
{
    public int OrderCount { get; set; }
    public decimal TotalRevenue { get; set; }
}

[DisallowConcurrentExecution]
public sealed class DailySalesReportJob(
    ILogger<DailySalesReportJob> logger) : IJob
{
    public static readonly JobKey JobKey = new(name: nameof(DailySalesReportJob),
        group: "reports");
    public static readonly TriggerKey TriggerKey = new(name: $"{nameof(DailySalesReportJob)}Trigger",
        group: "reports");

    public const string CronExpression = "0 0 0 * * ?"; // Every day at midnight
    internal const string Description = "Generates daily sales report for the previous day.";

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        logger.LogInformation(message: "Starting daily sales report generation");

        try
        {
            SalesReportData reportData = await GenerateDailySalesReport(cancellationToken: cancellationToken);
            logger.LogInformation(
                message: "Successfully generated daily sales report with {Orders} orders and total revenue {Revenue}",
                args:
                [
                    reportData.OrderCount,
                    reportData.TotalRevenue
                ]);
        }
        catch (Exception ex)
        {
            logger.LogError(exception: ex,
                message: "Error occurred during daily sales report generation");
            throw new JobExecutionException(cause: ex,
                refireImmediately: false);
        }
    }

    private async Task<SalesReportData> GenerateDailySalesReport(CancellationToken cancellationToken)
    {
        await Task.Delay(millisecondsDelay: 100,
            cancellationToken: cancellationToken);
        return new SalesReportData { OrderCount = 0, TotalRevenue = 0 };
    }
}
