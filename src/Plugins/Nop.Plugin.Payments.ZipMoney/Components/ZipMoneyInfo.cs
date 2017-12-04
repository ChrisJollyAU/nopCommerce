using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Payments.ZipMoney.Models;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.ZipMoney.Components
{
    [ViewComponent(Name = "ZipMoneyInfo")]
    public class ZipMoneyInfoViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            ZipInfoModel model = new ZipInfoModel();
            return View("~/Plugins/Payments.ZipMoney/Views/Info.cshtml",model);
        }
    }
}
