using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZipMoneySDK.Models
{
    public class ZipRefundRequest
    {
        [JsonProperty(Required = Required.Always)]
        public string charge_id { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string reason { get; set; }

        [JsonProperty(Required = Required.Always)]
        public decimal amount { get; set; }

        public Dictionary<string,string> metadata { get; set; }

        public bool ShouldSerializemetadata()
        {
            if (metadata == null) return false;
            if (metadata.Count == 0) return false;
            return true;
        }
    }
}
