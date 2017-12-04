using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Mvc.Models;
using System.Collections.Generic;

namespace Nop.Plugin.Payments.SecurePayAPI.Models
{
    public class PaymentInfoModel : BaseNopModel
    {
        public PaymentInfoModel()
        {
            ExpireMonths = new List<SelectListItem>();
            ExpireYears = new List<SelectListItem>();
            CardsAllowed = new List<SelectListItem>();
            SelectListItem mc = new SelectListItem
            {
                Text = "MasterCard"
            };
            SelectListItem visa = new SelectListItem
            {
                Text = "Visa"
            };
            CardsAllowed.Add(mc);
            CardsAllowed.Add(visa);
        }

        public string CardType { get; set; }

        [NopResourceDisplayName("Payment.CardNumber")]
        public string CardNumber { get; set; }

        [NopResourceDisplayName("Payment.ExpirationDate")]
        public string ExpireMonth { get; set; }

        [NopResourceDisplayName("Payment.ExpirationDate")]
        public string ExpireYear { get; set; }
        public IList<SelectListItem> ExpireMonths { get; set; }
        public IList<SelectListItem> ExpireYears { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SecurePayAPI.Fields.CardsAllowed")]
        public IList<SelectListItem> CardsAllowed { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SecurePayAPI.Fields.CVV")]
        public string CardCode { get; set; }
    }
}