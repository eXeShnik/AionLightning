using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AionLightning.Commons.Services.Cron;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;

namespace AionLightning.Commons.Services
{
    public class CronService
    {
        private readonly ILogger<CronService> _log;
        private IScheduler _scheduler;
        private Type _runnableRunner;

        public CronService(ILogger<CronService> log)
        {
            _log = log;
        }

        public async Task Init(Type runnableRunner)
        {
            if (_scheduler != null)
                return;

            if (runnableRunner == null)
                throw new CronServiceException("RunnableRunner class must be defined");

            _runnableRunner = runnableRunner;

            var properties = new System.Collections.Specialized.NameValueCollection
            {
                { "quartz.threadPool.threadCount", "1" }
            };

            try
            {
                var factory = new StdSchedulerFactory(properties);
                _scheduler = await factory.GetScheduler();
                await _scheduler.Start();
            }
            catch (SchedulerException e)
            {
                throw new CronServiceException("Failed to initialize CronService", e);
            }
        }

        public async Task Shutdown()
        {
            if (_scheduler == null)
                return;

            _log.LogInformation("CronService is shutting down");
            await _scheduler.Shutdown(true);
            _scheduler = null;
        }

        public async Task<bool> Schedule(IRunnable runnable, string cronExpression)
        {
            var jobName = runnable.GetType().Name;
            var jobGroup = "DEFAULT";

            var jobDetail = JobBuilder.Create(_runnableRunner)
                .WithIdentity(jobName, jobGroup)
                .UsingJobData("runnable", runnable.GetType().AssemblyQualifiedName)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity(jobName + "_trigger", jobGroup)
                .WithCronSchedule(cronExpression)
                .Build();

            try
            {
                await _scheduler.ScheduleJob(jobDetail, trigger);
                return true;
            }
            catch (SchedulerException e)
            {
                _log.LogError(e, "Failed to schedule job");
                return false;
            }
        }

        public async Task<bool> Deschedule(IRunnable runnable)
        {
            var jobName = runnable.GetType().Name;
            var jobGroup = "DEFAULT";

            try
            {
                await _scheduler.DeleteJob(new JobKey(jobName, jobGroup));
                return true;
            }
            catch (SchedulerException e)
            {
                _log.LogError(e, "Failed to deschedule job");
                return false;
            }
        }
    }

    public interface IRunnable
    {
        void Run();
    }
}
