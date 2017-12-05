using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZipMoneySDK.Models
{
    public class ZipTracking
    {
        public string uri { get; set; }

        public bool ShouldSerializeuri()
        {
            return !string.IsNullOrEmpty(uri);
        }

        public string number { get; set; }

        public bool ShouldSerializenumber()
        {
            return !string.IsNullOrEmpty(number);
        }

        public string carrier { get; set; }

        public bool ShouldSerializecarrier()
        {
            return !string.IsNullOrEmpty(carrier);
        }
    }
}
