using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZipMoneySDK.Models
{
    public class ZipRefundCollection
    {
        [JsonProperty(Required = Required.Always)]
        public List<ZipRefundResponse> items { get; set; }
    }
}
