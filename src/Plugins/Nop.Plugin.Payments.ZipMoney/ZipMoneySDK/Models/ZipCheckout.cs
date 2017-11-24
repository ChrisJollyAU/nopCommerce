using System.Collections.Generic;

namespace ZipMoneySDK.Models
{
    public class ZipCheckout
    {
        public ZipCheckout()
        {
            metadata = new Dictionary<string, string>();
        }
        public ZipShopper shopper { get; set; }
        public ZipOrder order { get; set; }
        public ZipFeatures features { get; set; }
        public ZipConfig config { get; set; }
        public Dictionary<string,string> metadata { get; set; }
    }
}
