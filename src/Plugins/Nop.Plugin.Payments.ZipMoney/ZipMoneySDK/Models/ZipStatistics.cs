using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipMoneySDK.Models
{
    public class ZipStatistics
    {
        public DateTime? account_created { get; set; }
        public bool ShouldSerializeaccount_created => account_created != null;

        public int sales_total_count { get; set; }
        public bool ShouldSerializesales_total_count => sales_total_count > 0;

        public decimal sales_total_amount { get; set; }
        public bool ShouldSerializesales_total_amount => sales_total_amount > 0;

        public decimal sales_avg_amount { get; set; }
        public bool ShouldSerializesales_avg_amount => sales_avg_amount > 0;

        public decimal sales_max_amount { get; set; }
        public bool ShouldSerializesales_max_amount => sales_max_amount > 0;

        public decimal refunds_total_amount { get; set; }
        public bool ShouldSerializerefunds_total_amount => refunds_total_amount > 0;

        public bool previous_chargeback { get; set; }
        public bool ShouldSerializeprevious_chargeback => previous_chargeback;

        public string currency { get; set; }
        public bool ShouldSerializecurrency => !string.IsNullOrEmpty(currency);

        public DateTime? last_login { get; set; }
        public bool ShouldSerializelast_login => last_login != null;

    }
}
