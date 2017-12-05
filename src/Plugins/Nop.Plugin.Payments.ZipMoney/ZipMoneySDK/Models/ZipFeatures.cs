using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipMoneySDK.Models
{
    public class ZipFeatures
    {
        public ZipTokenisation tokenisation { get; set; }
        public bool ShouldSerializetokenisation => tokenisation != null;
    }
}
