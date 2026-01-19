namespace AionLightning.Commons.Network
{
    public class DisconnectionTask : IRunnable
    {
        private readonly AConnection _connection;

        public DisconnectionTask(AConnection connection)
        {
            _connection = connection;
        }

        public void Run()
        {
            _connection.OnDisconnect();
        }
    }
}
