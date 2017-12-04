using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZipMoneySDK.Models
{
    public class ZipAddress
    {
        [JsonProperty(Required = Required.Always)]
        public string line1 { get; set; }
        public string line2 { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string city { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string state { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string postal_code { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string country { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
    }
}
