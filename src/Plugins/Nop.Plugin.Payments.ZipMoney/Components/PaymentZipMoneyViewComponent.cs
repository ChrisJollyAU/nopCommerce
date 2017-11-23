using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.ZipMoney.Components
{
    [ViewComponent(Name = "PaymentZipMoney")]
    public class PaymentZipMoneyViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Payments.ZipMoney/Views/PaymentInfo.cshtml");
        }
    }
}
