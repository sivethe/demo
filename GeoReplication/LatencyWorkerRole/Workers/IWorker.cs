using System;
using System.Threading;
using System.Threading.Tasks;

namespace LatencyWorkerRole.Workers
{
    interface IWorker
    {
        WorkerType Type { get; }

        string Region { get; }

        Task InitializeAsync();

        Task<Tuple<double, double>> RunAsync(CancellationToken token);

        Task CleanupAsync(CancellationToken token);
    }
}
