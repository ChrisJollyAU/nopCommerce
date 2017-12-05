using System.Collections.Generic;
using Newtonsoft.Json;

namespace ZipMoneySDK.Models
{
    public class ZipCheckout
    {
        public ZipCheckout()
        {
            metadata = new Dictionary<string, string>();
            shopper = new ZipShopper();
            order = new ZipOrder();
            config = new ZipConfig();
        }
        public string type { get; set; }
        public ZipShopper shopper { get; set; }
        [JsonProperty(Required = Required.Always)]
        public ZipOrder order { get; set; }
        public ZipFeatures features { get; set; }
        [JsonProperty(Required = Required.Always)]
        public ZipConfig config { get; set; }
        public Dictionary<string,string> metadata { get; set; }
    }
}
