using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.BrainTree.Models
{
    public class PaymentInfoModel : BaseNopModel
    {
        public string Token { get; set; }
        public decimal Amount { get; set; }
    }
}