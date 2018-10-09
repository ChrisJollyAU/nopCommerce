using Nop.Core;
using Nop.Core.Plugins;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;

namespace Nop.Plugin.Tracking.AfterShip
{
    public class AfterShipTrackingMethod : BasePlugin, IMiscPlugin
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;

        #endregion

        #region Ctor

        public AfterShipTrackingMethod(ISettingService settingService, IWebHelper webHelper)
        {
            this._settingService = settingService;
            this._webHelper = webHelper;

        }

        #endregion

        #region Methods

        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            this.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.ApiKey", "API Key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.ApiKey.Hint", "Specify AfterShip API Key.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.AfterShipUsername", "AfterShip Username");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.AfterShipUsername.Hint", "Specify AfterShip Username.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.AllowCustomerNotification", "Allow Customer Notification");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.AllowCustomerNotification.Hint", "Check to allow customer email\\sms notification from AfterShip service.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.Status.Pending", "New shipments added that are pending to track, or new shipments without tracking information available yet.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.Status.InfoReceived", "Carrier has received request from shipper and is about to pick up the shipment.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.Status.InTransit", "Carrier has accepted or picked up shipment from shipper. The shipment is on the way.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.Status.OutForDelivery", "Carrier is about to deliver the shipment , or it is ready to pickup.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.Status.AttemptFail", "Carrier attempted to deliver but failed, and usually leaves a notice and will try to delivery again.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.Status.Delivered", "The shipment was delivered successfully.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.Status.Expired", "Shipment has no tracking information for 7 days since added, or has no further updates for 30 days since last update.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.Status.Exception", "Custom hold, undelivered, returned shipment to sender or any shipping exceptions.");

            //settings
            var settings = new AfterShipSettings
            {
                AllowCustomerNotification = true,
                AfterShipUsername = "MyAfterShipUsername",
                ApiKey = "MyAfterShipAPIKey"
            };

            this._settingService.SaveSetting(settings);
            base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            this._settingService.DeleteSetting<AfterShipSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.ApiKey");
            this.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.ApiKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.AfterShipUsername");
            this.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.AfterShipUsername.Hint");
            this.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.AllowCustomerNotification");
            this.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.AllowCustomerNotification.Hint");
            this.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.Status.Pending");
            this.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.Status.InfoReceived");
            this.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.Status.InTransit");
            this.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.Status.OutForDelivery");
            this.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.Status.AttemptFail");
            this.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.Status.Delivered");
            this.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.Status.Expired");
            this.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.Status.Exception");

            base.Uninstall();
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/TrackingAfterShip/Configure";
        }

        #endregion
    }
}