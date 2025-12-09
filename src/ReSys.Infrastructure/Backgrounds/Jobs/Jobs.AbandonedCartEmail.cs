using Microsoft.Extensions.Logging;

using Quartz;

namespace ReSys.Infrastructure.Backgrounds.Jobs;

[DisallowConcurrentExecution]
public sealed class AbandonedCartEmailJob(
    ILogger<AbandonedCartEmailJob> logger) : IJob
{
    public static readonly JobKey JobKey = new(name: nameof(AbandonedCartEmailJob),
        group: "email");
    public static readonly TriggerKey TriggerKey = new(name: $"{nameof(AbandonedCartEmailJob)}Trigger",
        group: "email");

    public const string CronExpression = "0 0 * * * ?"; // Every hour at minute 0
    internal const string Description = "Sends reminder emails for carts abandoned for 24+ hours.";

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        logger.LogInformation(message: "Starting abandoned cart email job");

        try
        {
            int emailsSent = await SendAbandonedCartEmails(cancellationToken: cancellationToken);
            logger.LogInformation(message: "Successfully sent {Count} abandoned cart emails",
                args: emailsSent);
        }
        catch (Exception ex)
        {
            logger.LogError(exception: ex,
                message: "Error occurred during abandoned cart email processing");
            throw new JobExecutionException(cause: ex,
                refireImmediately: false);
        }
    }

    private static async Task<int> SendAbandonedCartEmails(CancellationToken cancellationToken)
    {
        await Task.Delay(millisecondsDelay: 100,
            cancellationToken: cancellationToken);
        return 0;
    }
}

