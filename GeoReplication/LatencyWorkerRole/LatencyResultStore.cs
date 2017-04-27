using LatencyWorkerRole.Workers;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace LatencyWorkerRole
{
    /// <summary>
    /// DocumentDB data store for latency information, which is queried by the WebApp
    /// </summary>
    internal sealed class LatencyResultStore
    {
        //DB and Coll details
        private const string DatabaseName = "nodetest";
        private const string MetricsCollectionName = "demometricsV2";
        
        //determines how much latency data points we have at any point
        private const int MetricsRecordCount = 100; 

        private DateTime lastMetricsCleanupTime = DateTime.MinValue;
        private TimeSpan MetricsCleanupInterval = new TimeSpan(0, 2, 0); //clean up metrics every 20 minutes        
        private MongoClient defaultClient;

        public LatencyResultStore()
        {
            this.SetupDefaultClient();
        }

        public async Task StoreLatencies(long iterationId, double writeLatency, double readLatency, string region, WorkerType workerType)
        {
            const string WriteLatencyField = "writeLatency";
            const string ReadLatencyField = "readLatency";
            const string RegionField = "region";
            const string TypeField = "type";
            const string IterationIdField = "iterationId";
            const string RunnerTypeField = "runnerType";
            const string LatencyInfoTypeStr = "latencyInfo";

            try
            {
                IMongoDatabase db = this.defaultClient.GetDatabase(LatencyResultStore.DatabaseName);
                IMongoCollection<BsonDocument> coll = db.GetCollection<BsonDocument>(LatencyResultStore.MetricsCollectionName);


                //store latency info
                BsonDocument latencyInfoDocument = new BsonDocument
                {
                    {IterationIdField, iterationId },
                    {RunnerTypeField, workerType.ToString() },
                    {TypeField, LatencyInfoTypeStr },
                    {WriteLatencyField, writeLatency },
                    {ReadLatencyField, readLatency },
                    {RegionField, region }
                };

                await coll.InsertOneAsync(latencyInfoDocument);

                //store worker region
                await this.StoreRegion();
                if (WorkerSettings.IsWriteRegion)
                {
                    //store account regions
                    await this.StoreAccountRegions();
                }

                if (DateTime.UtcNow - this.lastMetricsCleanupTime > this.MetricsCleanupInterval)
                {
                    //cleanup old metrics for each runner type
                    await this.CleanupMetrics();
                    this.lastMetricsCleanupTime = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Storing latencies failed exception : {0}", ex.ToString());
            }

        }
        
        //Default client setup and helper methods
        private void SetupDefaultClient()
        {
            try
            {
                MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(WorkerSettings.LatencyResultStoreConnectionString));
                settings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
                settings.ConnectionMode = ConnectionMode.ReplicaSet;
                settings.ReplicaSetName = "globaldb";
                this.defaultClient = new MongoClient(settings);
            }
            catch (Exception ex)
            {
                Trace.TraceError("setupMongoClient Failed with exception: {0} full exception: {1}", ex.GetType(), ex.ToString());
            }
        }

        //TODO: change as needed to required protocol
        private async Task StoreRegion()
        {
            const string TypeField = "type";
            const string RegionInfoTypeStr = "regionInfo";
            const string RegionField = "region";
            const string IsWriteRegionField = "iswriteregion";

            try
            {
                IMongoDatabase db = this.defaultClient.GetDatabase(LatencyResultStore.DatabaseName);
                IMongoCollection<BsonDocument> coll = db.GetCollection<BsonDocument>(LatencyResultStore.MetricsCollectionName);

                //update region info
                BsonDocument regionInfoDocument = new BsonDocument
                {
                    {TypeField, RegionInfoTypeStr },
                    {RegionField, WorkerSettings.CurrentRegion },
                    {IsWriteRegionField, WorkerSettings.IsWriteRegion }
                };

                var filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq(TypeField, RegionInfoTypeStr),
                    Builders<BsonDocument>.Filter.Eq(RegionField, WorkerSettings.CurrentRegion));
                FindOneAndReplaceOptions<BsonDocument> options = new FindOneAndReplaceOptions<BsonDocument>();
                options.IsUpsert = true;
                await coll.FindOneAndReplaceAsync<BsonDocument>(filter, regionInfoDocument, options);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Storing worker role region failed  exception ; {0}", ex.ToString());
            }
        }

        //TODO: change as needed to required protocol
        private async Task CleanupMetrics()
        {
            const string TypeField = "type";
            const string LatencyInfoTypeStr = "latencyInfo";
            const string RegionField = "region";

            try
            {
                IMongoDatabase db = this.defaultClient.GetDatabase(LatencyResultStore.DatabaseName);
                IMongoCollection<BsonDocument> coll = db.GetCollection<BsonDocument>(LatencyResultStore.MetricsCollectionName);

                var builder = Builders<BsonDocument>.Filter;
                var filter1 = builder.Eq(TypeField, LatencyInfoTypeStr);
                var filter2 = builder.Eq(RegionField, WorkerSettings.CurrentRegion);
                List<FilterDefinition<BsonDocument>> flist = new List<FilterDefinition<BsonDocument>>();
                flist.Add(filter1);
                flist.Add(filter2);
                var filter = builder.And(flist);
                var sort = Builders<BsonDocument>.Sort.Descending("_id");

                long rcount = coll.Find(filter).Count();
                if (rcount > LatencyResultStore.MetricsRecordCount)
                {
                    var doc = coll.Find(filter).Sort(sort).Skip(LatencyResultStore.MetricsRecordCount).FirstOrDefault<BsonDocument>();
                    BsonValue id;
                    if (doc.TryGetValue("_id", out id))
                    {
                        var dbuilder = Builders<BsonDocument>.Filter;
                        var dfilter1 = builder.Lt<ObjectId>("_id", id.AsObjectId);
                        var dfilter2 = builder.Eq(TypeField, LatencyInfoTypeStr);
                        var dfilter3 = builder.Eq(RegionField, WorkerSettings.CurrentRegion);
                        List<FilterDefinition<BsonDocument>> dlist = new List<FilterDefinition<BsonDocument>>();
                        dlist.Add(dfilter1);
                        dlist.Add(dfilter2);
                        dlist.Add(dfilter3);
                        var dfilter = builder.And(dlist);
                        var dresult = await coll.DeleteManyAsync(dfilter);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Metrics cleanup failed exception - {0}", ex.ToString());
            }
        }

        //TODO: change as needed to required protocol
        private async Task StoreAccountRegions()
        {
            const string TypeField = "type";
            const string AccRegionInfoTypeStr = "AccRegionInfo";
            const string RegionsField = "regions";

            //ismaster
            BsonDocument isMasterResponse = await this.GetIsMasterResponse();

            List<string> regions = GetAccountRegions(isMasterResponse);
            BsonDocument doc = new BsonDocument
            {
                {TypeField, AccRegionInfoTypeStr},
                {RegionsField, new BsonArray(regions)}
            };

            try
            {
                IMongoDatabase db = this.defaultClient.GetDatabase(LatencyResultStore.DatabaseName);
                IMongoCollection<BsonDocument> coll = db.GetCollection<BsonDocument>(LatencyResultStore.MetricsCollectionName);

                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq(TypeField, AccRegionInfoTypeStr);
                FindOneAndReplaceOptions<BsonDocument> options = new FindOneAndReplaceOptions<BsonDocument>();
                options.IsUpsert = true;
                await coll.FindOneAndReplaceAsync<BsonDocument>(filter, doc, options);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Storing Account regions failed exception : {0}", ex.ToString());
            }

        }

        //TODO: change as needed to required protocol
        private async Task<BsonDocument> GetIsMasterResponse()
        {
            BsonDocument isMasterReponse = null;
            try
            {
                //run isMaster command using default client
                IMongoDatabase db = this.defaultClient.GetDatabase(LatencyResultStore.DatabaseName);
                var command = new BsonDocumentCommand<BsonDocument>(new BsonDocument
                    {
                        {"isMaster", 1}
                    });

                isMasterReponse = await db.RunCommandAsync(command);
            }
            catch (Exception ex)
            {
                Trace.TraceError("IsMaster failed  exception {0}, full exception - {1}", ex.GetType(), ex.ToString());
            }

            return isMasterReponse;
        }

        private List<string> GetAccountRegions(BsonDocument isMasterResponse)
        {
            const string HostsElement = "hosts";
            List<string> regions = new List<string>();
            string regionValue = string.Empty;

            //post process result to retrive endpoint
            BsonValue hostsVal;
            if (isMasterResponse.TryGetValue(HostsElement, out hostsVal))
            {
                //populate result
                BsonArray hosts = hostsVal.AsBsonArray;
                foreach (var host in hosts)
                {
                    string endpoint = host.AsString;
                    int index = endpoint.IndexOf(':');
                    if (index != -1)
                    {
                        endpoint = endpoint.Substring(0, index);
                    }

                    regions.Add(AzureRegionsMap.GetRegionName(endpoint));
                }
            }

            return regions;
        }
    }
}
