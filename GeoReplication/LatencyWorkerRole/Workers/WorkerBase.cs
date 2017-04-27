using System;
using System.Threading;
using System.Threading.Tasks;
using LatencyWorkerRole.Workers;
using System.Collections.Generic;
using System.Diagnostics;

namespace LatencyWorkerRole
{
    internal abstract class WorkerBase : IWorker
    {
        private const int OperationCount = 10;

        public abstract WorkerType Type { get; }

        public abstract string Region { get; }

        public abstract Task CleanupAsync(CancellationToken token);

        public abstract Task InitializeAsync();

        public abstract Task<Tuple<double, double>> RunAsync(CancellationToken token);

        private double MeasureLatency(Action action)
        {
            double latency;
            List<double> latencies = new List<Double>();
            Stopwatch latencyWatch = Stopwatch.StartNew();
            for (int i = 0; i < WorkerBase.OperationCount; i++)
            {
                latencyWatch.Restart();
                action();
                latencyWatch.Stop();
                latencies.Add(latencyWatch.ElapsedMilliseconds * 1.0);
            }

            latency = this.GetP99Latency(latencies);
            return latency;
        }

        protected double MeasureLatencySafe(Action action, string actionDesc)
        {
            try
            {
                return MeasureLatency(action);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Latency action failed for action desc: {0} Type: {1} exception:{2}", actionDesc, this.Type, ex.ToString());
            }

            return 0.0;
        }

        private double GetP99Latency(List<Double> latencies)
        {
            latencies.Sort();
            int N = latencies.Count;
            int p99Index = (99 * N / 100) - 1;
            return latencies[p99Index];
        }
    }
}
