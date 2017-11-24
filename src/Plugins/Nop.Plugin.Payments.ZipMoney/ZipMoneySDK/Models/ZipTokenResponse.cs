using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipMoneySDK.Models
{
    public class ZipTokenResponse
    {
        public string id { get; set; }
        public string value { get; set; }
        public bool active { get; set; }
        public DateTime created_date { get; set; }
    }
}
