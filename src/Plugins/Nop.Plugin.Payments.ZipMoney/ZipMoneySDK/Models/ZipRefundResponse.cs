using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipMoneySDK.Models
{
    public class ZipRefundResponse
    {
        public ZipRefundResponse()
        {
            metadata = new Dictionary<string, string>();
        }
        public string id { get; set; }
        public string charge_id { get; set; }
        public string reason { get; set; }
        public decimal amount { get; set; }
        public DateTime created { get; set; }
        public Dictionary<string,string> metadata { get; set; }
    }
}
