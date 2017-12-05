using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZipMoneySDK.Models
{
    public class ZipErrorContainer
    {
        [JsonProperty(Required = Required.Always)]
        public ZipError error { get; set; }
    }
}
