using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.DiscountRules.ShippingCountry
{
    public partial class RouteProvider : IRouteProvider
    {
        #region Methods

        public void RegisterRoutes(IRouteBuilder routeBuilder)
        {
            routeBuilder.MapRoute("Plugin.DiscountRules.ShippingCountry.Configure",
                 "Plugins/DiscountRulesShippingCountry/Configure",
                 new { controller = "DiscountRulesShippingCountry", action = "Configure" });
        }

        #endregion

        #region Properties
        
        public int Priority
        {
            get
            {
                return 0;
            }
        }

        #endregion
    }
}
