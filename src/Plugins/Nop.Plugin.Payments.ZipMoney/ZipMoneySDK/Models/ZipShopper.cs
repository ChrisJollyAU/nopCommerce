using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipMoneySDK.Models
{
    public class ZipShopper
    {
        public string title { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string middle_name { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string birth_date { get; set; }
        public string gender { get; set; }
        public ZipStatistics statistics { get; set; }
        public ZipAddress billing_address { get; set; }
    }
}
