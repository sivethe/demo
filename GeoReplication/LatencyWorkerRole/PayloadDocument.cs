using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LatencyWorkerRole
{
    internal sealed class PayloadDocument
    {
        public PayloadDocument(int length, string type)
        {
            this.Ud = this.GeneratePayloadDocIdHelper();
            this.Type = this.GeneratePayloadTypeHelper(type);
            this.payload = this.GeneratePayloadHelper(length);
        }

        [JsonProperty(PropertyName = "ud")]
        public string Ud { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "payload")]
        public string payload;

        private string GeneratePayloadDocIdHelper()
        {
            string documentId = String.Format(CultureInfo.InvariantCulture, "LatencyDoc {0}", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            return documentId;
        }

        private string GeneratePayloadTypeHelper(string type)
        {
            string typeString = String.Format(CultureInfo.InvariantCulture, "Type {0}", type);
            return typeString;
        }

        private string GeneratePayloadHelper(int length)
        {
            int curlength = this.ToString().Length;
            if (length - curlength < 0)
            {
                throw new ArgumentException("payload length");
            }

            string payload = new String(Enumerable.Repeat('X', length - curlength).ToArray());
            return payload;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{{ud:{0},type:{1},payload:{2}}}", this.Ud, this.Type, this.payload);
        }
    }
}
