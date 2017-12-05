using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZipMoneySDK.Models
{
    public class ZipRefundResponse
    {
        [JsonProperty(Required = Required.Always)]
        public string id { get; set; }

        public string charge_id { get; set; }

        public bool ShouldSerializecharge_id()
        {
            return !string.IsNullOrEmpty(charge_id);
        }

        [JsonProperty(Required = Required.Always)]
        public string reason { get; set; }

        [JsonProperty(Required = Required.Always)]
        public decimal amount { get; set; }

        [JsonProperty(Required = Required.Always)]
        public DateTime created { get; set; }

        public Dictionary<string,string> metadata { get; set; }

        public bool ShouldSerializemetadata()
        {
            if (metadata == null) return false;
            if (metadata.Count == 0) return false;
            return true;
        }
    }
}
