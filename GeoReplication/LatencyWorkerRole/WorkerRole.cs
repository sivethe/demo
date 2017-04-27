using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using LatencyWorkerRole.Workers;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Security.Authentication;

namespace LatencyWorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private const int CooldownIntervalinMS = 2000; //2 seconds
        
        private LatencyResultStore resultStore = new LatencyResultStore();
        private List<IWorker> workers = new List<IWorker>();

        public override void Run()
        {
            Trace.TraceInformation("LatencyWorkerRole is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            //Initialize latency result store
            this.resultStore = new LatencyResultStore();

            // Create and initialize worker instances
            //IWorker mongoWorker = new MongoWorker(WorkerSettings.CurrentRegion);
            //mongoWorker.InitializeAsync().Wait();
            IWorker tableNativeWorker = new TableWorker(WorkerSettings.TableNativeConnectionString, WorkerType.TablesNative);
            tableNativeWorker.InitializeAsync().Wait();
            IWorker tablev2Worker = new TableWorker(WorkerSettings.TablePremiumConnectionString, WorkerType.Tablesv2);
            tablev2Worker.InitializeAsync().Wait();

            //Add to list 
            //this.workers.Add(mongoWorker);
            this.workers.Add(tableNativeWorker);
            this.workers.Add(tablev2Worker);

            bool result = base.OnStart();
            Trace.TraceInformation("LatencyWorkerRole has been started");
            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("LatencyWorkerRole is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("LatencyWorkerRole has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {            
            while (!cancellationToken.IsCancellationRequested)
            {
                long iteration = DateTime.UtcNow.Ticks;
                foreach (var runner in this.workers)
                {
                    Tuple<double, double> latencyVals = await runner.RunAsync(cancellationToken);
                    //Item1 - write latency
                    //Item2 - read latency

                    //reporting phase
                    await this.resultStore.StoreLatencies(iteration, latencyVals.Item1, latencyVals.Item2, WorkerSettings.CurrentRegion, runner.Type);
                    await runner.CleanupAsync(cancellationToken);
                }

                Thread.Sleep(WorkerRole.CooldownIntervalinMS); //cool down
            }
        }
    }
}
