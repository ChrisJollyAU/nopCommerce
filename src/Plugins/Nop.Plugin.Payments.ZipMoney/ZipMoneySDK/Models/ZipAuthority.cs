using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ZipMoneySDK.Models
{
    public enum AuthorityType
    {
        checkout_id,store_code,account_token
    }
    public class ZipAuthority
    {
        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public AuthorityType type { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string value { get; set; }
    }
}
