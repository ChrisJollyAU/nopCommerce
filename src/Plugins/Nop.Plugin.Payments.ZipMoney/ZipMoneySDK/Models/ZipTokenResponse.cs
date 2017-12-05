using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZipMoneySDK.Models
{
    public class ZipTokenResponse
    {
        [JsonProperty(Required = Required.Always)]
        public string id { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string value { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool active { get; set; }

        [JsonProperty(Required = Required.Always)]
        public DateTime created_date { get; set; }
    }
}
