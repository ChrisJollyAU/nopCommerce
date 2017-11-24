using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipMoneySDK.Models
{
    public class ZipStatistics
    {
        public DateTime account_created { get; set; }
        public int sales_total_count { get; set; }
        public decimal sales_total_amount { get; set; }
        public decimal sales_avg_amount { get; set; }
        public decimal sales_max_amount { get; set; }
        public decimal refunds_total_amount { get; set; }
        public bool previous_chargeback { get; set; }
        public string currency { get; set; }
        public DateTime last_login { get; set; }
    }
}
