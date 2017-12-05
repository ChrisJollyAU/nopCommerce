using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Plugin.Payments.SecurePayAPI.Models;
using Nop.Web.Framework.Components;
using System.Linq;

namespace Nop.Plugin.Payments.SecurePay.Components
{
    [ViewComponent(Name = "PaymentSecurePay")]
    public class PaymentSecurePayViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var model = new PaymentInfoModel();

            //years
            for (int i = 0; i < 15; i++)
            {
                string year = Convert.ToString(DateTime.Now.Year + i);
                model.ExpireYears.Add(new SelectListItem()
                {
                    Text = year,
                    Value = year,
                });
            }

            //months
            for (int i = 1; i <= 12; i++)
            {
                string text = (i < 10) ? "0" + i.ToString() : i.ToString();
                model.ExpireMonths.Add(new SelectListItem()
                {
                    Text = text,
                    Value = i.ToString(),
                });
            }
            return View("~/Plugins/Payments.SecurePayAPI/Views/PaymentInfo.cshtml", model);
        }
    }
}
