using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.SecurePay
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("Plugin.Payments.SecurePayAPI.Configure",
                 "Plugins/PaymentSecurePayAPI/Configure",
                 new { controller = "PaymentSecurePayAPI", action = "Configure" },
                 new[] { "Nop.Plugin.Payments.SecurePayAPI.Controllers" }
            );

            routes.MapRoute("Plugin.Payments.SecurePayAPI.PaymentInfo",
                 "Plugins/PaymentSecurePay/PaymentInfoAPI",
                 new { controller = "PaymentSecurePayAPI", action = "PaymentInfo" },
                 new[] { "Nop.Plugin.Payments.SecurePayAPI.Controllers" }
            );

            //Cancel
            routes.MapRoute("Plugin.Payments.SecurePayAPI.CancelOrder",
                 "Plugins/PaymentSecurePayAPI/CancelOrder",
                 new { controller = "PaymentSecurePayAPI", action = "CancelOrder" },
                 new[] { "Nop.Plugin.Payments.SecurePayAPI.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
