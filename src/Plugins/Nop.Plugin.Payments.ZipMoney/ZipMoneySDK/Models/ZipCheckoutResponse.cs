using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipMoneySDK.Models
{
    public class ZipCheckoutResponse
    {
        public string id { get; set; }
        public string uri { get; set; }
        public string type { get; set; }
        public ZipShopper shopper { get; set; }
        public ZipOrder order { get; set; }
        public ZipFeatures features { get; set; }
        public Dictionary<string, string> metadata { get; set; }
        public DateTime created { get; set; }
        public string state { get; set; }
        public ZipConfig config { get; set; }
    }
}
