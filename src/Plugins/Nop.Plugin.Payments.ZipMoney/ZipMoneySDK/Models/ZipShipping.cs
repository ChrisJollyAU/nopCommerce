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
        }
        public bool pickup { get; set; }
        public bool ShouldSerializepckup => pickup;

        public ZipTracking tracking { get; set; }
        public bool ShouldSerializetracking => tracking != null;

        public ZipAddress address { get; set; }
        public bool ShouldSerializeaddress => address != null;

    }
}
