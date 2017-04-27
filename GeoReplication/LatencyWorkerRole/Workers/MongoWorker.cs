using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace LatencyWorkerRole.Workers
{
    internal sealed class MongoWorker : WorkerBase
    {
        private const int CooldownIntervalinMS = 2000; //2 seconds
        private const int PayloadLength = 1024;
        private const string MongoReadPayloadTypeStr = "readPayloadMongo";
        private const string MongoWritePayloadTypeStr = "writePayloadMongo";
        private const int MongoPort = 10250;
        private const string MongoUserName = "mongodemovishi";
        private const string MongoPassword = "N3rSZw2zbXKmvy4Dc8BH4fphy9YCoxesncWBbPLNKB0IGLz7cs57DISQ1U9Fx1D27H70JTd13hboxDUXD03tmw==";
        private const string MongoDefaultEndpoint = "mongodemovishi.documents.azure.com";
        
        //DB and Coll details
        private const string DatabaseName = "nodetest";
        private const string DataCollectionName = "demodataV2";

        private const string FindActionDescStr = "findAction";
        private const string InsertActionDescStr = "insertAction";

        private MongoClient client;
        //payload related
        private PayloadDocument insertPayloadDoc = new PayloadDocument(MongoWorker.PayloadLength, MongoWorker.MongoWritePayloadTypeStr);
        private PayloadDocument readPayloadDoc = new PayloadDocument(MongoWorker.PayloadLength, MongoWorker.MongoReadPayloadTypeStr);
                
        public MongoWorker(string region)
        {
            this.Region = region;
        }

        public override WorkerType Type { get { return WorkerType.MongoDB; } }

        public override string Region { get; }

        public override async Task CleanupAsync(CancellationToken token)
        {
            //delete all insert docs
            IMongoDatabase db = this.client.GetDatabase(MongoWorker.DatabaseName);
            IMongoCollection<PayloadDocument> coll = db.GetCollection<PayloadDocument>(MongoWorker.DataCollectionName);
            var builder = Builders<PayloadDocument>.Filter;
            var filter = builder.Eq("Type", this.insertPayloadDoc.Type);
            try
            {
                await coll.DeleteManyAsync(filter);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Cleanup failed ex: {0}, full ex:{1}", ex.GetType(), ex.ToString());
            }
        }

        public override async Task InitializeAsync()
        {
            this.SetupClient();
            await this.PopulateReadDocument();
        }

        public override async Task<Tuple<double, double>> RunAsync(CancellationToken token)
        {
            //ismaster
            BsonDocument isMasterResponse = await GetIsMasterResponse();
            bool isWriteRegion = this.IsWriteRegion(isMasterResponse);

            //measurement phase
            //latency actions
            IMongoDatabase dbr = this.client.GetDatabase(MongoWorker.DatabaseName);
            IMongoCollection<BsonDocument> collr = dbr.GetCollection<BsonDocument>(MongoWorker.DataCollectionName);

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("ud", this.readPayloadDoc.Ud);
            var tagList = new List<TagSet>();
            var tags = new List<Tag>();
            tags.Add(new Tag("region", this.Region));
            tagList.Add(new TagSet(tags));
            ReadPreference readPref = new ReadPreference(ReadPreferenceMode.Nearest, tagList);

            double readLatency = this.MeasureLatencySafe(
                () =>
                {
                    var fResult = collr.WithReadPreference(readPref).Find<BsonDocument>(filter).FirstOrDefault<BsonDocument>();
                }, MongoWorker.FindActionDescStr);

            Thread.Sleep(MongoWorker.CooldownIntervalinMS); //cool down

            double writeLatency = 0.0;
            if (isWriteRegion) //measure write latency only at write region
            {
                IMongoDatabase db = this.client.GetDatabase(MongoWorker.DatabaseName);
                IMongoCollection<PayloadDocument> coll = db.GetCollection<PayloadDocument>(MongoWorker.DataCollectionName);
                writeLatency = this.MeasureLatencySafe(
                    () =>
                    {
                        coll.InsertOne(this.insertPayloadDoc);
                    }, MongoWorker.InsertActionDescStr);
            }

            return new Tuple<double, double>(writeLatency, readLatency);
        }

        private void SetupClient()
        {
            try
            {
                this.client = new MongoClient(this.GetMongoClientSettingsHelper(MongoWorker.MongoDefaultEndpoint));
            }
            catch (Exception ex)
            {
                Trace.TraceError("setupMongoClient Failed with exception: {0} full exception: {1}", ex.GetType(), ex.ToString());
            }
        }

        private MongoClientSettings GetMongoClientSettingsHelper(string endpoint)
        {
            const string MongoAuthScramSHATypeString = "SCRAM-SHA-1";

            string mongoHost = endpoint;
            string mongoUsername = MongoWorker.MongoUserName;
            string mongoPassword = MongoWorker.MongoPassword;

            MongoClientSettings settings = new MongoClientSettings();
            settings.Server = new MongoServerAddress(mongoHost, MongoWorker.MongoPort);
            settings.UseSsl = true;
            settings.VerifySslCertificate = true;
            settings.SslSettings = new SslSettings();
            settings.SslSettings.EnabledSslProtocols = SslProtocols.Tls12;

            settings.ConnectionMode = ConnectionMode.ReplicaSet;
            settings.ReplicaSetName = "globaldb";

            MongoIdentity identity = new MongoInternalIdentity(MongoWorker.DatabaseName, mongoUsername);
            MongoIdentityEvidence evidence = new PasswordEvidence(mongoPassword);

            settings.Credentials = new List<MongoCredential>()
            {
                new MongoCredential(MongoAuthScramSHATypeString, identity, evidence)
            };

            return settings;
        }

        private async Task PopulateReadDocument()
        {
            try
            {
                this.client = new MongoClient(this.GetMongoClientSettingsHelper(MongoWorker.MongoDefaultEndpoint));

                //insert read payload document once to be used by read operations
                IMongoDatabase db = this.client.GetDatabase(MongoWorker.DatabaseName);
                IMongoCollection<PayloadDocument> coll = db.GetCollection<PayloadDocument>(MongoWorker.DataCollectionName);
                await coll.InsertOneAsync(this.readPayloadDoc);
            }
            catch (Exception ex)
            {
                Trace.TraceError("setupMongoClient Failed with exception: {0} full exception: {1}", ex.GetType(), ex.ToString());
            }
        }

        private async Task<BsonDocument> GetIsMasterResponse()
        {
            BsonDocument isMasterReponse = null;
            try
            {
                //run isMaster command using default client
                IMongoDatabase db = this.client.GetDatabase(MongoWorker.DatabaseName);
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

        private bool IsWriteRegion(BsonDocument isMasterResponse)
        {
            const string primaryElement = "primary";

            //post process result to retrive writeRegion
            BsonValue primaryval;
            if (isMasterResponse.TryGetValue(primaryElement, out primaryval))
            {
                string primaryRegion = AzureRegionsMap.GetRegionName(primaryval.AsString);
                if (primaryRegion.Equals(this.Region))
                {
                    return true;
                }

            }

            return false;
        }
    }
}
