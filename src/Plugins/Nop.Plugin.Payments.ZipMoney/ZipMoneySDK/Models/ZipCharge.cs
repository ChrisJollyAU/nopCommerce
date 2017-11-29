using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipMoneySDK.Models
{
    public class ZipCharge
    {
        public ZipCharge()
        {
            authority = new ZipAuthority();
        }
        public ZipAuthority authority { get; set; }
        public string reference { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public bool capture { get; set; }
        public ZipOrder order { get; set; }

        public bool ShouldSerializeorder()
        {
            return order != null;
        }
    }
}
