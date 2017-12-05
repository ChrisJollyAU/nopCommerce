using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ZipMoneySDK.Models
{
    public enum OrderType {
        sku,tax,shipping,discount
    }

    public class ZipOrderItem
    {
        [JsonProperty(Required = Required.Always)]
        public string name { get; set; }

        [JsonProperty(Required = Required.Always)]
        public decimal amount { get; set; }

        public string reference { get; set; }
        public bool ShouldSerializereference => !string.IsNullOrEmpty(reference);

        public string description { get; set; }
        public bool ShouldSerializedescription => !string.IsNullOrEmpty(description);

        [JsonProperty(Required = Required.Always)]
        public int quantity { get; set; }

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public OrderType type { get; set; }

        public string image_uri {get; set;}
        public bool ShouldSerializeimage_uri => !string.IsNullOrEmpty(image_uri);

        public string item_uri { get; set; }
        public bool ShouldSerializeitem_uri => !string.IsNullOrEmpty(item_uri);

        public string product_code { get; set; }
        public bool ShouldSerializeproduct_code => !string.IsNullOrEmpty(product_code);
    }
}
