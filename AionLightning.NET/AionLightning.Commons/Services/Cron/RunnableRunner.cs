using System.Threading.Tasks;
using Quartz;

namespace AionLightning.Commons.Services.Cron
{
    public abstract class RunnableRunner : IJob
    {
        public abstract Task Execute(IJobExecutionContext context);
    }
}
