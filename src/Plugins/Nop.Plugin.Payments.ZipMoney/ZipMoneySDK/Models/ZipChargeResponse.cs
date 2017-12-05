using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ZipMoneySDK.Models
{
    public enum ChargeState
    {
        authorised,captured,cancelled,refunded
    }
    public class ZipChargeResponse
    {
        [JsonProperty(Required = Required.Always)]
        public string id { get; set; }

        public string reference { get; set; }

        [JsonProperty(Required = Required.Always)]
        public decimal amount { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string currency { get; set; }

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public ChargeState state { get; set; }

        [JsonProperty(Required = Required.Always)]
        public decimal captured_amount { get; set; }

        [JsonProperty(Required = Required.Always)]
        public decimal refunded_amount { get; set; }

        [JsonProperty(Required = Required.Always)]
        public DateTime created_date { get; set; }

        public ZipOrder order { get; set; }

        public bool ShouldSerializeorder()
        {
            return order != null;
        }

        public Dictionary<string,string> metadata { get; set; }

        public bool ShouldSerializemetadata()
        {
            if (metadata == null) return false;
            if (metadata.Count == 0) return false;
            return true;
        }
    }
}
