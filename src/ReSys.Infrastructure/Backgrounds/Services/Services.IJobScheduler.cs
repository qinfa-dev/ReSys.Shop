using Quartz;

namespace ReSys.Infrastructure.Backgrounds.Services;
/// <summary>
/// Service for scheduling and triggering background jobs on-demand
/// </summary>
public interface IJobSchedulerService
{
    /// <summary>
    /// Triggers a job immediately with optional parameters
    /// </summary>
    Task TriggerJobAsync<TJob>(JobDataMap? dataMap = null, CancellationToken cancellationToken = default)
        where TJob : IJob;

    /// <summary>
    /// Schedules a job to run at a specific time
    /// </summary>
    Task ScheduleJobAsync<TJob>(DateTimeOffset startAt, JobDataMap? dataMap = null, CancellationToken cancellationToken = default)
        where TJob : IJob;

    /// <summary>
    /// Schedules a job to run after a delay
    /// </summary>
    Task ScheduleJobAsync<TJob>(TimeSpan delay, JobDataMap? dataMap = null, CancellationToken cancellationToken = default)
        where TJob : IJob;
}