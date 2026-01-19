using System.Collections.Generic;
using System.Threading;
using AionLightning.Commons.Network.Packet;
using Microsoft.Extensions.Logging;

namespace AionLightning.Commons.Network
{
    public class PacketProcessor<T> where T : AConnection
    {
        private static readonly ILogger<PacketProcessor<T>> _log = new LoggerFactory().CreateLogger<PacketProcessor<T>>();
        private readonly List<Queue<BaseClientPacket>> _queues;
        private readonly List<ThreadPool> _pools;
        private readonly int _poolSize;

        public PacketProcessor(int poolSize, int queueSize, int maxPacketSize, int maxQueueSize)
        {
            _poolSize = poolSize;
            _queues = new List<Queue<BaseClientPacket>>(queueSize);
            for (int i = 0; i < queueSize; i++)
            {
                _queues.Add(new Queue<BaseClientPacket>());
            }

            _pools = new List<ThreadPool>(poolSize);
            for (int i = 0; i < poolSize; i++)
            {
                var pool = new ThreadPool(this, i);
                _pools.Add(pool);
                var thread = new Thread(pool.Run);
                thread.Start();
            }
        }

        public void ExecutePacket(BaseClientPacket packet)
        {
            var queue = _queues[packet.GetClient().GetHashCode() % _queues.Count];
            lock (queue)
            {
                queue.Enqueue(packet);
            }
        }

        private class ThreadPool
        {
            private readonly PacketProcessor<T> _processor;
            private readonly int _poolId;

            public ThreadPool(PacketProcessor<T> processor, int poolId)
            {
                _processor = processor;
                _poolId = poolId;
            }

            public void Run()
            {
                while (true)
                {
                    for (int i = _poolId; i < _processor._queues.Count; i += _processor._poolSize)
                    {
                        var queue = _processor._queues[i];
                        if (queue.Count > 0)
                        {
                            BaseClientPacket packet = null;
                            lock (queue)
                            {
                                if (queue.Count > 0)
                                {
                                    packet = queue.Dequeue();
                                }
                            }

                            if (packet != null)
                            {
                                packet.Run();
                            }
                        }
                    }

                    Thread.Sleep(1);
                }
            }
        }
    }
}
