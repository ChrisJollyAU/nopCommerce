using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZipMoneySDK.Models
{
    public class ZipChargeRequest
    {
        [JsonProperty(Required = Required.Always)]
        public ZipAuthority authority { get; set; }

        public string reference { get; set; }
        public bool ShouldSerializereference => !string.IsNullOrEmpty(reference);

        [JsonProperty(Required = Required.Always)]
        public decimal amount { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string currency { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool capture { get; set; }

        public ZipOrder order { get; set; }

        public bool ShouldSerializeorder()
        {
            return order != null;
        }
    }
}
