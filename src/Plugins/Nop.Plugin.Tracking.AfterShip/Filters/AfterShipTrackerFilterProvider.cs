using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Web.Controllers;

namespace Nop.Plugin.Tracking.AfterShip.Filters
{
    public class AfterShipTrackerFilterProvider : IFilterProvider
    {
        #region Methods

        /* public IEnumerable<Filter> GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
         {
             if (actionDescriptor.ControllerDescriptor.ControllerType == typeof(Web.Controllers.OrderController)
                 && actionDescriptor.ActionName.Equals("ShipmentDetails"))
             {
                 return new[]
                 {
                     new Filter(new AfterShipTrackerWebFilterAttribute(), FilterScope.Action, null)
                 };
             }

             if (actionDescriptor.ControllerDescriptor.ControllerType == typeof(Admin.Controllers.OrderController)
                 && actionDescriptor.ActionName.Equals("ShipmentDetails"))
             {
                 return new[]
                 {
                     new Filter(new AfterShipTrackerAdminFilterAttribute(), FilterScope.Action, null)
                 };
             }

             return new Filter[] { };
         }*/

        #endregion

        public int Order => 1;

        public void OnProvidersExecuted(FilterProviderContext context)
        {
            
        }

        public void OnProvidersExecuting(FilterProviderContext context)
        {
            var actionName = context.ActionContext.RouteData.Values["action"].ToString();

            if (!actionName.Equals("ShipmentDetails"))
                return;

            var controller = (context.ActionContext as Microsoft.AspNetCore.Mvc.ControllerContext)?.ActionDescriptor.ControllerTypeInfo;

            if (controller == typeof(OrderController))
            {
                context.Results.Add(new FilterItem(
                    new FilterDescriptor(new AfterShipTrackerWebFilterAttribute(), FilterScope.Action), new AfterShipTrackerWebFilterAttribute()));
            }
            if (controller == typeof(Web.Areas.Admin.Controllers.OrderController))
            {
                context.Results.Add(new FilterItem(
                    new FilterDescriptor(new AfterShipTrackerAdminFilterAttribute(), FilterScope.Action), new AfterShipTrackerAdminFilterAttribute()));
            }
        }
    }
}