using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.ZipMoney.Models
{
    public class ZipReferConfirmModel
    {
        public bool ShowInfo { get; set; }

        public string Info1 { get; set; }
        public string Info2 { get; set; }
        public string Info3 { get; set; }
    }
}
