using System.Collections.Generic;

namespace ZipMoneySDK.Models
{
    public class ZipCheckout
    {
        public ZipCheckout()
        {
            metadata = new Dictionary<string, string>();
            shopper = new ZipShopper();
            order = new ZipOrder();
            features = new ZipFeatures();
            config = new ZipConfig();
        }
        public ZipShopper shopper { get; set; }
        public ZipOrder order { get; set; }
        public ZipFeatures features { get; set; }
        public ZipConfig config { get; set; }
        public Dictionary<string,string> metadata { get; set; }
    }
}
