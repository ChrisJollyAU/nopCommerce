using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZipMoneySDK.Models
{
    public class ZipOrder
    {
        public ZipOrder()
        {
            items = new List<ZipOrderItem>();
            shipping = new ZipShipping();
            items = new List<ZipOrderItem>();
        }
        public string reference { get; set; }
        public bool ShouldSerializereference => !string.IsNullOrEmpty(reference);

        [JsonProperty(Required = Required.Always)]
        public decimal amount { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string currency { get; set; }

        [JsonProperty(Required = Required.Always)]
        public ZipShipping shipping { get; set; }

        public List<ZipOrderItem> items { get; set; }

        public bool ShouldSerializeitems
        {
            get
            {
                if (items == null) return false;
                if (items.Count == 0) return false;
                return true;
            }
        }
    }
}
