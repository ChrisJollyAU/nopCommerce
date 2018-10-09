using Nop.Core.Configuration;

namespace Nop.Plugin.Tracking.AfterShip
{
    public class AfterShipSettings : ISettings
    {
        public string ApiKey { get; set; }
        public string AfterShipUsername { get; set; }
        public bool AllowCustomerNotification { get; set; }
    }
}