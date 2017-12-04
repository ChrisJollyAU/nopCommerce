using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Payments.ZipMoney.Models;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.ZipMoney.Components
{
    [ViewComponent(Name = "ZipMoneyProductPage")]
    public class ZipMoneyProductPageComponent : NopViewComponent
    {
        private readonly ZipMoneyPaymentSettings _zipMoneyPaymentSettings;

        public ZipMoneyProductPageComponent(ZipMoneyPaymentSettings zipMoneyPaymentSettings)
        {
            _zipMoneyPaymentSettings = zipMoneyPaymentSettings;
        }
        public IViewComponentResult Invoke()
        {
            ZipMoneyWidget model = new ZipMoneyWidget();
            if (_zipMoneyPaymentSettings.UseSandbox)
            {
                model.environment = "sandbox";
                model.PublicKey = _zipMoneyPaymentSettings.SandboxPublicKey;
            }
            else
            {
                model.environment = "production";
                model.PublicKey = _zipMoneyPaymentSettings.ProductionPublicKey;
            }
            return View("~/Plugins/Payments.ZipMoney/Views/ZipMoneyProductPage.cshtml",model);
        }
    }
}
