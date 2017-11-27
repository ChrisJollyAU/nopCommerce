using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipMoneySDK.Models
{
    public class ZipOrderItem
    {
        public string name { get; set; }
        public decimal amount { get; set; }
        public int quantity { get; set; }
        public string type { get; set; }
        public string reference { get; set; }
        public string image_uri {get; set;}
        public string item_uri { get; set; }
    }
}
