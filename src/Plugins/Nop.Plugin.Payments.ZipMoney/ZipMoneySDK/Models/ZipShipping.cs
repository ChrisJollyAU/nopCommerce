using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipMoneySDK.Models
{
    public class ZipShipping
    {
        public ZipShipping()
        {
            tracking = new ZipTracking();
            address = new ZipAddress();
        }
        public bool pickup { get; set; }
        public ZipTracking tracking { get; set; }
        public ZipAddress address { get; set; }
    }
}
