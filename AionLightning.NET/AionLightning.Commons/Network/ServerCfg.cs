namespace AionLightning.Commons.Network
{
    public class ServerCfg
    {
        public readonly string HostName;
        public readonly int Port;
        public readonly string ConnectionName;
        public readonly IConnectionFactory Factory;

        public ServerCfg(string hostName, int port, string connectionName, IConnectionFactory factory)
        {
            HostName = hostName;
            Port = port;
            ConnectionName = connectionName;
            Factory = factory;
        }
    }
}
