using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZipMoneySDK.Models
{
    public class ZipError
    {
        [JsonProperty(Required = Required.Always)]
        public string code { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string message { get; set; }

        public List<ZipErrorDetail> details { get; set; }
        public bool ShouldSerializedetails()
        {
            if (details == null) return false;
            if (details.Count == 0) return false;
            return true;
        }
    }
}
