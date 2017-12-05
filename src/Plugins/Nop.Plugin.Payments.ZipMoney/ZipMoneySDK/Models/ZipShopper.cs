using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;

namespace ZipMoneySDK.Models
{
    public enum Gender
    {
        Male,Female,Other
    }
    public class ZipShopper
    {
        public ZipShopper()
        {
            statistics = new ZipStatistics();
            billing_address = new ZipAddress();
        }
        public string title { get; set; }
        public bool ShouldSerializetitle => !string.IsNullOrEmpty(title);

        [JsonProperty(Required = Required.Always)]
        public string first_name { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string last_name { get; set; }

        public string middle_name { get; set; }
        public bool ShouldSerializemiddle_name => !string.IsNullOrEmpty(middle_name);

        public string phone { get; set; }
        public bool ShouldSerializephone => !string.IsNullOrEmpty(phone);

        [JsonProperty(Required = Required.Always)]
        public string email { get; set; }

        public string birth_date { get; set; }
        public bool ShouldSerializebirth_date => !string.IsNullOrEmpty(birth_date);

        [JsonConverter(typeof(StringEnumConverter))]
        public Gender? gender { get; set; }
        public bool ShouldSerializegender => gender != null;

        public ZipStatistics statistics { get; set; }
        public bool ShouldSerializestatistics => statistics != null;

        [JsonProperty(Required = Required.Always)]
        public ZipAddress billing_address { get; set; }
    }
}
