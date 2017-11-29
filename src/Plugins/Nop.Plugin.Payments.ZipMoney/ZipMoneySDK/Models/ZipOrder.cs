using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipMoneySDK.Models
{
    public class ZipOrder
    {
        public ZipOrder()
        {
            items = new List<ZipOrderItem>();
            shipping = new ZipShipping();
            items = new List<ZipOrderItem>();
        }
        public string reference { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public ZipShipping shipping { get; set; }
        public List<ZipOrderItem> items { get; set; }
    }
}
