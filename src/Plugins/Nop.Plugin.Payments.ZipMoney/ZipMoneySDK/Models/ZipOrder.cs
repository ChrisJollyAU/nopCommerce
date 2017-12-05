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
        public string reference { get; set; }

        public bool ShouldSerializereference()
        {
            return !string.IsNullOrEmpty(reference);
        }

        [JsonProperty(Required = Required.Always)]
        public ZipShipping shipping { get; set; }

        public List<ZipOrderItem> items { get; set; }

        public bool ShouldSerializeitems()
        {
            if (items == null) return false;
            if (items.Count == 0) return false;
            return true;
        }

        public string cart_reference { get; set; }

        public bool ShouldSerializecart_reference()
        {
            return !string.IsNullOrEmpty(cart_reference);
        }
    }
}
