using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Tracking.AfterShip.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Tracking.AfterShip.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    public class TrackingAfterShipController : BasePluginController
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;

        #endregion

        #region Ctor

        public TrackingAfterShipController(
            ISettingService settingService,
            ILocalizationService localizationService)
        {
            this._settingService = settingService;
            this._localizationService = localizationService;
        }

        #endregion

        #region Methods

        public IActionResult Configure()
        {
            var afterShipSettings = _settingService.LoadSetting<AfterShipSettings>();
            var model = new ConfigurationModel
            {
                AfterShipUsername = afterShipSettings.AfterShipUsername,
                AllowCustomerNotification = afterShipSettings.AllowCustomerNotification,
                ApiKey = afterShipSettings.ApiKey
            };

            return View("~/Plugins/Tracking.AfterShip/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public IActionResult Configure(ConfigurationModel model)
        {
            //load settings
            var afterShipSettings = _settingService.LoadSetting<AfterShipSettings>();
            afterShipSettings.AllowCustomerNotification = model.AllowCustomerNotification;
            afterShipSettings.AfterShipUsername = model.AfterShipUsername;
            afterShipSettings.ApiKey = model.ApiKey;

            _settingService.SaveSetting(afterShipSettings);

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        #endregion
    }
}