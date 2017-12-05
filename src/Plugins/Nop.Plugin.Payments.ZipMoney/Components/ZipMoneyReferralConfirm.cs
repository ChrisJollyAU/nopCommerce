using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Payments.ZipMoney.Models;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.ZipMoney.Components
{
    [ViewComponent(Name = "ZipMoneyReferralConfirm")]
    public class ZipMoneyReferralConfirmComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            ZipReferConfirmModel model = new ZipReferConfirmModel();
            string referral1 = HttpContext.Session.GetString("ZipReferred1");
            string referral2 = HttpContext.Session.GetString("ZipReferred2");
            string referral3 = HttpContext.Session.GetString("ZipReferred3");
            int? showmsg = HttpContext.Session.GetInt32("ZipShowReferred");
            if (showmsg.HasValue && !string.IsNullOrEmpty(referral1))
            {
                model.Info1 = referral1;
                model.Info2 = referral2;
                model.Info3 = referral3;
                model.ShowInfo = showmsg.Value == 1;
                HttpContext.Session.Remove("ZipReferred1");
                HttpContext.Session.Remove("ZipReferred2");
                HttpContext.Session.Remove("ZipReferred3");
                HttpContext.Session.Remove("ZipShowReferred");
            }
            return View("~/Plugins/Payments.ZipMoney/Views/ReferralConfirm.cshtml",model);
        }
    }
}
