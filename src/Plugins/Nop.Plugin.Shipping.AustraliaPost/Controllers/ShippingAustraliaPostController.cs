using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Shipping.AustraliaPost.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Shipping.AustraliaPost.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    public class ShippingAustraliaPostController : BasePluginController
    {
        #region Fields

        private readonly AustraliaPostSettings _australiaPostSettings;
        private readonly ILocalizationService _localizationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly INotificationService _notificationService;
        #endregion

        #region Ctor

        public ShippingAustraliaPostController(AustraliaPostSettings australiaPostSettings,
            ILocalizationService localizationService,
            IPermissionService permissionService,
            INotificationService notificationService,
            ISettingService settingService)
        {
            this._australiaPostSettings = australiaPostSettings;
            this._localizationService = localizationService;
            this._permissionService = permissionService;
            this._settingService = settingService;
            _notificationService = notificationService;
        }

        #endregion

        #region Methods

        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var model = new AustraliaPostShippingModel()
            {
                ApiKey = _australiaPostSettings.ApiKey,
                AdditionalHandlingCharge = _australiaPostSettings.AdditionalHandlingCharge
            };

            return View("~/Plugins/Shipping.AustraliaPost/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public IActionResult Configure(AustraliaPostShippingModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();
            
            //save settings
            _australiaPostSettings.ApiKey = model.ApiKey;
            _australiaPostSettings.AdditionalHandlingCharge = model.AdditionalHandlingCharge;
            _settingService.SaveSetting(_australiaPostSettings);

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        #endregion
    }
}
