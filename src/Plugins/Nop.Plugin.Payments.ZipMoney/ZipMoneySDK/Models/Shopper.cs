using System.Collections.Generic;

namespace Nop.Plugin.Payments.ZipMoney.ZipMoneySDK.Models
{
    public class Shopper
    {
        public Shopper()
        {
            metadata = new Dictionary<string, string>();
        }
        public string title { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string middle_name { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string birth_date { get; set; }
        public string gender { get; set; }
        public Statistics statistics { get; set; }
        public ZipAddress billing_address { get; set; }
        public ZipOrder order { get; set; }
        public ZipFeatures features { get; set; }
        public ZipConfig config { get; set; }
        public Dictionary<string,string> metadata { get; set; }
    }
}
