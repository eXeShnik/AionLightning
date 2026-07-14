using System.Collections.Concurrent;
using System.Collections.Specialized;
using AionLightning.Commons.Services.Cron;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;

namespace AionLightning.Commons.Services;

/// <summary>
/// Port of Java <c>commons.services.CronService</c> — schedules recurring server tasks (siege start
/// times, housing auction/maintenance sweeps, etc.) from a standard Quartz cron expression
/// (e.g. <c>"0 5 12 ? * SUN"</c>). Java wrapped a raw <see cref="Quartz"/>-equivalent
/// (org.quartz) <c>Scheduler</c> behind a <c>RunnableRunner</c> job that dispatched onto its own
/// thread pool; this port schedules directly against Quartz.NET's <see cref="IScheduler"/> and uses
/// an internal <see cref="CronActionJob"/> to invoke the supplied delegate, so no bridging job class
/// needs to be registered by the caller (Java's <c>CronService.initSingleton(RunnableRunner.class)</c>
/// step has no equivalent here).
/// </summary>
public sealed class CronService
{
    private const string JobGroup = "cron";

    private readonly ILogger<CronService> _log;
    private readonly ConcurrentDictionary<Action, JobKey> _jobsByAction = new();
    private IScheduler? _scheduler;

    public CronService(ILogger<CronService> log)
    {
        _log = log;
    }

    /// <summary>Starts the underlying Quartz scheduler. Safe to call once; subsequent calls are no-ops.</summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_scheduler != null)
            return;

        var properties = new NameValueCollection
        {
            { "quartz.threadPool.threadCount", "4" }
        };

        try
        {
            var factory = new StdSchedulerFactory(properties);
            _scheduler = await factory.GetScheduler(ct);
            await _scheduler.Start(ct);
            _log.LogInformation("CronService started");
        }
        catch (SchedulerException e)
        {
            throw new CronServiceException("Failed to initialize CronService", e);
        }
    }

    /// <summary>Stops the scheduler, waiting for in-flight (non-long-running) jobs to complete.</summary>
    public async Task StopAsync(CancellationToken ct = default)
    {
        if (_scheduler is null)
            return;

        _log.LogInformation("CronService is shutting down");
        await _scheduler.Shutdown(waitForJobsToComplete: true, ct);
        _scheduler = null;
    }

    /// <summary>
    /// Schedules <paramref name="task"/> to run on every fire time of <paramref name="cronExpression"/>
    /// (standard Quartz cron syntax). When <paramref name="longRunningTask"/> is true the task is
    /// dispatched onto the thread pool (<see cref="Task.Run(Action)"/>) instead of being awaited inline,
    /// mirroring Java's <c>RunnableRunner.executeLongRunningRunnable</c> vs. <c>executeRunnable</c>
    /// distinction (Java dispatched onto its own <c>ThreadPoolManager</c>; here it's the default pool).
    /// </summary>
    public async Task<bool> Schedule(Action task, string cronExpression, bool longRunningTask = false)
    {
        EnsureStarted();

        var jobKey = new JobKey($"cron-{Guid.NewGuid():N}", JobGroup);
        var jobDetail = JobBuilder.Create<CronActionJob>()
            .WithIdentity(jobKey)
            .Build();
        jobDetail.JobDataMap.Put(CronActionJob.ActionKey, task);
        jobDetail.JobDataMap.Put(CronActionJob.LongRunningKey, longRunningTask);

        var trigger = TriggerBuilder.Create()
            .WithIdentity(jobKey.Name + "-trigger", JobGroup)
            .WithCronSchedule(cronExpression)
            .Build();

        try
        {
            await _scheduler!.ScheduleJob(jobDetail, trigger);
            _jobsByAction[task] = jobKey;
            return true;
        }
        catch (SchedulerException e)
        {
            _log.LogError(e, "Failed to schedule cron job with expression {CronExpression}", cronExpression);
            return false;
        }
    }

    /// <summary>Cancels a job previously scheduled with the same <see cref="Action"/> delegate.</summary>
    public Task<bool> Cancel(Action task)
    {
        if (!_jobsByAction.TryRemove(task, out var jobKey))
            return Task.FromResult(false);

        return CancelJob(jobKey);
    }

    private async Task<bool> CancelJob(JobKey jobKey)
    {
        EnsureStarted();
        try
        {
            return await _scheduler!.DeleteJob(jobKey);
        }
        catch (SchedulerException e)
        {
            _log.LogError(e, "Failed to cancel cron job {JobKey}", jobKey);
            return false;
        }
    }

    private void EnsureStarted()
    {
        if (_scheduler is null)
            throw new CronServiceException("CronService is not started");
    }
}

/// <summary>
/// Quartz job that invokes the <see cref="Action"/> delegate stashed in its <see cref="JobDataMap"/>
/// by <see cref="CronService.Schedule"/>. Internal — callers only ever interact with <see cref="CronService"/>.
/// </summary>
internal sealed class CronActionJob : IJob
{
    public const string ActionKey = "action";
    public const string LongRunningKey = "longRunning";

    public Task Execute(IJobExecutionContext context)
    {
        var data = context.JobDetail.JobDataMap;
        if (data.Get(ActionKey) is not Action action)
            return Task.CompletedTask;

        if (data.GetBoolean(LongRunningKey))
            _ = Task.Run(action);
        else
            action();

        return Task.CompletedTask;
    }
}
