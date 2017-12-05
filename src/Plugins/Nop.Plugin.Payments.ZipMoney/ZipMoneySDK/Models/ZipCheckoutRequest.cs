using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ZipMoneySDK.Models
{
    public enum CheckoutType
    {
        standard,
        express
    }

    public class ZipCheckoutRequest
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public CheckoutType type { get; set; }
        public bool ShouldSerializetype => type != CheckoutType.standard;

        public ZipShopper shopper { get; set; }
        public bool ShouldSerializeshopper => shopper != null;

        [JsonProperty(Required = Required.Always)]
        public ZipOrder order { get; set; }

        public ZipFeatures features { get; set; }
        public bool ShouldSerializefeatures => features != null;

        [JsonProperty(Required = Required.Always)]
        public ZipConfig config { get; set; }

        public Dictionary<string,string> metadata { get; set; }
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
