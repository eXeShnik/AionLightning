using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace AionLightning.Commons.Utils.Concurrent
{
    public static class RunnableStatsManager
    {
        private static readonly ConcurrentDictionary<Type, ClassStat> ClassStats = new ConcurrentDictionary<Type, ClassStat>();

        private class ClassStat
        {
            private readonly string _className;
            private readonly MethodStat _runnableStat;

            private readonly ConcurrentDictionary<string, MethodStat> _methodStats;

            public ClassStat(Type clazz)
            {
                _className = clazz.FullName.Replace("AionLightning.", "");
                _runnableStat = new MethodStat(_className, "Run()");
                _methodStats = new ConcurrentDictionary<string, MethodStat>();
                _methodStats.TryAdd("Run()", _runnableStat);
            }

            public MethodStat GetRunnableStat()
            {
                return _runnableStat;
            }

            public MethodStat GetMethodStat(string methodName)
            {
                if (methodName == "Run()")
                    return _runnableStat;

                return _methodStats.GetOrAdd(methodName, new MethodStat(_className, methodName));
            }

            public IEnumerable<MethodStat> GetMethodStats()
            {
                return _methodStats.Values;
            }
        }

        private class MethodStat
        {
            private readonly string _className;
            private readonly string _methodName;

            private long _count;
            private long _total;
            private long _min = long.MaxValue;
            private long _max = long.MinValue;

            public MethodStat(string className, string methodName)
            {
                _className = className;
                _methodName = methodName;
            }

            public void AddTime(long time)
            {
                Interlocked.Increment(ref _count);
                Interlocked.Add(ref _total, time);
                long currentMin, currentMax;
                do
                {
                    currentMin = _min;
                } while (time < currentMin && Interlocked.CompareExchange(ref _min, time, currentMin) != currentMin);
                do
                {
                    currentMax = _max;
                } while (time > currentMax && Interlocked.CompareExchange(ref _max, time, currentMax) != currentMax);
            }

            public override string ToString()
            {
                return $"{_className}.{_methodName}: count={_count}, total={_total}, min={_min}, max={_max}, avg={(_count > 0 ? (double)_total / _count : 0)}";
            }
        }

        public static void AddRunnableStats(Type clazz, long runTime)
        {
            var classStat = ClassStats.GetOrAdd(clazz, new ClassStat(clazz));
            classStat.GetRunnableStat().AddTime(runTime);
        }

        public static void AddMethodStats(Type clazz, string methodName, long runTime)
        {
            var classStat = ClassStats.GetOrAdd(clazz, new ClassStat(clazz));
            classStat.GetMethodStat(methodName).AddTime(runTime);
        }

        public static string GetStats()
        {
            var sb = new StringBuilder();
            var stats = ClassStats.Values.SelectMany(cs => cs.GetMethodStats()).OrderBy(ms => ms.ToString());
            foreach (var stat in stats)
            {
                sb.AppendLine(stat.ToString());
            }
            return sb.ToString();
        }
    }
}
