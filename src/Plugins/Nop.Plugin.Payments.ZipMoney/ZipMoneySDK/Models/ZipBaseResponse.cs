using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipMoneySDK.Models
{
    public class ZipBaseResponse
    {
        public ZipBaseResponse()
        {
            metadata = new Dictionary<string, string>();
        }
        public string id { get; set; }
        public string reference { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public string state { get; set; }
        public decimal captured_amount { get; set; }
        public decimal refunded_amount { get; set; }
        public DateTime created_date { get; set; }
        public Dictionary<string,string> metadata { get; set; }
    }
}
