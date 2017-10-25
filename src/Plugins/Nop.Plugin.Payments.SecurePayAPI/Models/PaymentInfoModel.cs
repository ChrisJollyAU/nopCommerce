using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.SecurePayAPI.Models
{
    public class PaymentInfoModel : BaseNopModel
    {
        public PaymentInfoModel()
        {
            ExpireMonths = new List<SelectListItem>();
            ExpireYears = new List<SelectListItem>();
            CardsAllowed = new List<SelectListItem>();
            SelectListItem mc = new SelectListItem();
            mc.Text = "MasterCard";
            SelectListItem visa = new SelectListItem();
            visa.Text = "Visa";
            CardsAllowed.Add(mc);
            CardsAllowed.Add(visa);
        }

        public string CardType { get; set; }

        [NopResourceDisplayName("Payment.CardNumber")]
        [AllowHtml]
        public string CardNumber { get; set; }

        [NopResourceDisplayName("Payment.ExpirationDate")]
        [AllowHtml]
        public string ExpireMonth { get; set; }

        [NopResourceDisplayName("Payment.ExpirationDate")]
        [AllowHtml]
        public string ExpireYear { get; set; }
        public IList<SelectListItem> ExpireMonths { get; set; }
        public IList<SelectListItem> ExpireYears { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SecurePayAPI.Fields.CardsAllowed")]
        [AllowHtml]
        public IList<SelectListItem> CardsAllowed { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SecurePayAPI.Fields.CVV")]
        [AllowHtml]
        public string CardCode { get; set; }
    }
}