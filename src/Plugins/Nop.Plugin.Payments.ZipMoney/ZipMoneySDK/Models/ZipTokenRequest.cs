using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZipMoneySDK.Models
{
    public class ZipTokenRequest
    {
        [JsonProperty(Required = Required.Always)]
        public ZipAuthority authority { get; set; }
    }
}
