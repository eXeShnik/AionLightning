using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AionLightning.Commons.Network.Util
{
    public class DeadLockDetector
    {
        private readonly ILogger<DeadLockDetector> _log;
        private readonly Dictionary<Thread, StackTrace> _threadStacks = new Dictionary<Thread, StackTrace>();

        public DeadLockDetector(ILogger<DeadLockDetector> log)
        {
            _log = log;
        }

        public void Run()
        {
            // This method is not implemented in the original code
        }
    }
}
