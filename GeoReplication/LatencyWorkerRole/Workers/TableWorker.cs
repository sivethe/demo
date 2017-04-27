using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LatencyWorkerRole.Workers
{
    internal sealed class TableWorker : WorkerBase
    {
        private const string TableName = "LatencyTestTable";
    
        private readonly WorkerType type;
        private readonly CloudTable table;

        public TableWorker(string connectionString, WorkerType type)
        {
            CloudTableClient client = CloudStorageAccount.Parse(connectionString).CreateCloudTableClient();
            this.table = client.GetTableReference(TableWorker.TableName);
            this.type = type;
        }

        public override WorkerType Type { get { return this.type; } }

        public override string Region { get => throw new NotImplementedException(); }

        public override async Task CleanupAsync(CancellationToken token)
        {
            if (WorkerSettings.IsWriteRegion)
            {
                var query = from latencyEntity in this.table.CreateQuery<TablePayload>()
                            where latencyEntity.PartitionKey == "writePartitionKey"
                            select latencyEntity;
                foreach (TablePayload latencyEntity in query)
                {
                    // Execute the operation.
                    TableOperation deleteOperation = TableOperation.Delete(latencyEntity);
                    await table.ExecuteAsync(deleteOperation);
                    Trace.TraceInformation("Deleted entity {0} {1}", latencyEntity.RowKey, latencyEntity.PartitionKey);
                }
            }
        }

        public async override Task InitializeAsync()
        {
            await this.table.CreateIfNotExistsAsync();
            if (WorkerSettings.IsWriteRegion)
            {
                TablePayload payload = new TablePayload(this.Type, "readPartitionKey", WorkerSettings.CurrentRegion);
                TableOperation insertOperation = TableOperation.InsertOrReplace(payload);
                this.table.Execute(insertOperation);
            }
        }

        public override Task<Tuple<double, double>> RunAsync(CancellationToken token)
        {
            double writeLatency = 0.0;
            double readLatency = 0.0;
            if (WorkerSettings.IsWriteRegion)
            {
                writeLatency = this.MeasureLatencySafe(
                    () => 
                    {
                        TablePayload payload = new TablePayload(this.Type, "writePartitionKey", Guid.NewGuid().ToString());
                        TableOperation insertOperation = TableOperation.Insert(payload);
                        this.table.Execute(insertOperation);
                    }, "insert");
            }

            readLatency = this.MeasureLatencySafe(
                () =>
                {
                    TableOperation readOperation = TableOperation.Retrieve<TablePayload>("readPartitionKey", WorkerSettings.CurrentRegion);
                    TableResult result = this.table.Execute(readOperation);
                }, "read");

            return Task.FromResult<Tuple<double, double>>(new Tuple<double, double>(writeLatency, readLatency));
        }
    }

    internal sealed class TablePayload : TableEntity
    {
        public TablePayload()
            : base()
        {
        }

        public TablePayload(WorkerType type, string partitionKey, string rowKey)
            : base(partitionKey, rowKey)
        {
            this.WorkerType = type.ToString();
            this.Payload = new PayloadDocument(1024, "Table").ToString();
            this.WorkerType = type.ToString();
        }

        public string Payload { get; set; }

        public string WorkerType { get; set; }
    }
}
