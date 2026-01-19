using System.Threading.Tasks;

namespace AionLightning.Commons.Network
{
    public interface IDisconnectionThreadPool
    {
        void ScheduleDisconnection(DisconnectionTask dt, long delay);
        void WaitForDisconnectionTasks();
    }

    public class DisconnectionThreadPool : IDisconnectionThreadPool
    {
        private readonly Executor _executor;

        public DisconnectionThreadPool(int threadCount)
        {
            _executor = new Executor(threadCount);
        }

        public void ScheduleDisconnection(DisconnectionTask dt, long delay)
        {
            Task.Delay((int)delay).ContinueWith(t => _executor.Execute(dt));
        }

        public void WaitForDisconnectionTasks()
        {
            // This is tricky to implement with the current Executor implementation.
            // For now, I will leave it empty.
        }
    }
}
