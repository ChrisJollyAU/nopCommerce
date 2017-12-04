using Microsoft.AspNetCore.Http;
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
            string error = HttpContext.Session.GetString("ZipFriendlyError");
            int? showerror = HttpContext.Session.GetInt32("ZipShowError");
            if (showerror.HasValue && !string.IsNullOrEmpty(error))
            {
                model.Info = error;
                model.ShowInfo = showerror.Value == 1;
                HttpContext.Session.Remove("ZipFriendlyError");
                HttpContext.Session.Remove("ZipShowError");
            }
            return View("~/Plugins/Payments.ZipMoney/Views/Info.cshtml",model);
        }
    }
}
