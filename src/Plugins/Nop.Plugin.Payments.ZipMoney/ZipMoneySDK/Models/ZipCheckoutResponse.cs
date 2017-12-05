using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ZipMoneySDK.Models
{
    public enum CheckoutState
    {
        created,
        expired,
        approved,
        completed
    }

    public class ZipCheckoutResponse
    {
        [JsonProperty(Required = Required.Always)]
        public string id { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string uri { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public CheckoutType type { get; set; }
        public bool ShouldSerializetype => type != CheckoutType.standard;

        public ZipShopper shopper { get; set; }
        public bool ShouldSerializeshopper => shopper != null;

        public ZipOrder order { get; set; }
        public bool ShouldSerializeorder => order != null;

        public ZipFeatures features { get; set; }
        public bool ShouldSerializefeatures => features != null;

        public ZipConfig config { get; set; }
        public bool ShouldSerializeconfig => config != null;

        [JsonProperty(Required = Required.Always)]
        public DateTime created { get; set; }

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public CheckoutState state { get; set; }

        public string redirect_uri { get; set; }

        public Dictionary<string, string> metadata { get; set; }

        public bool ShouldSerializemetadata
        {
            get
            {
                if (metadata == null) return false;
                if (metadata.Count == 0) return false;
                return true;
            }
        }
    }
}
