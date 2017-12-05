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
        public string title { get; set; }

        public bool ShouldSerializetitle()
        {
            return !string.IsNullOrEmpty(title);
        }

        [JsonProperty(Required = Required.Always)]
        public string first_name { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string last_name { get; set; }

        public string middle_name { get; set; }

        public bool ShouldSerializemiddle_name()
        {
            return !string.IsNullOrEmpty(middle_name);
        }

        public string phone { get; set; }

        public bool ShouldSerializephone()
        {
            return !string.IsNullOrEmpty(phone);
        }

        [JsonProperty(Required = Required.Always)]
        public string email { get; set; }

        public DateTime? birth_date { get; set; }

        public bool ShouldSerializebirth_date()
        {
            return birth_date != null;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public Gender? gender { get; set; }

        public bool ShouldSerializegender()
        {
            return gender != null;
        }

        public ZipStatistics statistics { get; set; }

        public bool ShouldSerializestatistics()
        {
            return statistics != null;
        }

        [JsonProperty(Required = Required.Always)]
        public ZipAddress billing_address { get; set; }
    }
}
