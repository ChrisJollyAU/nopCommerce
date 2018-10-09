using Autofac;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Tracking.AfterShip.Filters;

namespace Nop.Plugin.Tracking.AfterShip.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public int Order { get { return 1; } }

        public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            //FilterProviders.Providers.Add(new AfterShipTrackerFilterProvider());
            builder.RegisterType<AfterShipTrackerFilterProvider>().As<IFilterProvider>().InstancePerLifetimeScope();
        }
    }
}