using System.ComponentModel.DataAnnotations;

namespace ReSys.Infrastructure.Backgrounds.Options;

public sealed class BackgroundServicesOptions
{
    public const string Section = "BackgroundServices:Quartz";

    /// <summary>
    /// Enable or disable Quartz dashboard (if using a dashboard plugin)
    /// </summary>
    public bool EnableDashboard { get; init; } = false;

    /// <summary>
    /// Dashboard URL path (e.g., "/quartz", "/admin/jobs")
    /// </summary>
    [Required]
    public string DashboardPath { get; init; } = "/quartz";

    /// <summary>
    /// Quartz server configuration
    /// </summary>
    [Required]
    public QuartzServerOptions Server { get; init; } = new();
}

public sealed class QuartzServerOptions
{
    /// <summary>
    /// Maximum number of concurrent jobs (thread pool size)
    /// </summary>
    [Range(minimum: 1, maximum: 50)]
    public int MaxConcurrency { get; init; } = 10;

    /// <summary>
    /// Job history retention in days
    /// </summary>
    [Range(minimum: 1, maximum: 365)]
    public int JobRetentionDays { get; init; } = 7;

    /// <summary>
    /// Misfire threshold in seconds (how long before considering a job misfired)
    /// </summary>
    [Range(minimum: 1, maximum: 300)]
    public int MisfireThresholdSeconds { get; init; } = 60;
}