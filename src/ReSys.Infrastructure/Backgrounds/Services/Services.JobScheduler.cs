using Quartz;

namespace ReSys.Infrastructure.Backgrounds.Services;

/// <summary>
/// Implementation of job scheduling service using Quartz.NET
/// </summary>
public sealed class JobSchedulerService(IScheduler scheduler) : IJobSchedulerService
{
    public async Task TriggerJobAsync<TJob>(JobDataMap? dataMap = null, CancellationToken cancellationToken = default)
        where TJob : IJob
    {
        var jobKey = new JobKey(name: typeof(TJob).Name);

        var trigger = TriggerBuilder.Create()
            .WithIdentity(name: $"{typeof(TJob).Name}-{Guid.NewGuid()}")
            .ForJob(jobKey: jobKey)
            .StartNow();

        if (dataMap != null)
        {
            trigger.UsingJobData(newJobDataMap: dataMap);
        }

        await scheduler.ScheduleJob(trigger: trigger.Build(),
            cancellationToken: cancellationToken);
    }

    public async Task ScheduleJobAsync<TJob>(DateTimeOffset startAt, JobDataMap? dataMap = null, CancellationToken cancellationToken = default)
        where TJob : IJob
    {
        var jobKey = new JobKey(name: typeof(TJob).Name);

        var trigger = TriggerBuilder.Create()
            .WithIdentity(name: $"{typeof(TJob).Name}-{Guid.NewGuid()}")
            .ForJob(jobKey: jobKey)
            .StartAt(startTimeUtc: startAt);

        if (dataMap != null)
        {
            trigger.UsingJobData(newJobDataMap: dataMap);
        }

        await scheduler.ScheduleJob(trigger: trigger.Build(),
            cancellationToken: cancellationToken);
    }

    public async Task ScheduleJobAsync<TJob>(TimeSpan delay, JobDataMap? dataMap = null, CancellationToken cancellationToken = default)
        where TJob : IJob
    {
        var startAt = DateTimeOffset.UtcNow.Add(timeSpan: delay);
        await ScheduleJobAsync<TJob>(startAt: startAt,
            dataMap: dataMap,
            cancellationToken: cancellationToken);
    }
}