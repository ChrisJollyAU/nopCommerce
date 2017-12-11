using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Directory;
using Nop.Plugin.Shipping.Temando.Models;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Shipping.Temando.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    public class ShippingTemandoController : BasePluginController
    {
        private readonly TemandoSettings _temandoSettings;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly CurrencySettings _currencySettings;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly IMeasureService _measureService;
        private readonly IPermissionService _permissionService;
        private readonly IShippingService _shippingService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreService _storeService;
        private readonly MeasureSettings _measureSettings;

        public ShippingTemandoController(TemandoSettings temandoSettings, CurrencySettings currencySettings,
            ICountryService countryService,
            ICurrencyService currencyService,
            ILocalizationService localizationService,
            IMeasureService measureService,
            IPermissionService permissionService,
            ISettingService settingService,
            IShippingService shippingService,
            IStateProvinceService stateProvinceService,
            IStoreService storeService,
            MeasureSettings measureSettings)
        {
            this._temandoSettings = temandoSettings;
            this._settingService = settingService;
            this._localizationService = localizationService;
            this._currencySettings = currencySettings;
            this._countryService = countryService;
            this._currencyService = currencyService;
            this._localizationService = localizationService;
            this._measureService = measureService;
            this._permissionService = permissionService;
            this._settingService = settingService;
            this._stateProvinceService = stateProvinceService;
            this._shippingService = shippingService;
            this._storeService = storeService;
            this._measureSettings = measureSettings;
        }

        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var model = new TemandoShippingModel();
            model.Username = _temandoSettings.Username;
            model.Password = _temandoSettings.Password;
            model.Threshold = _temandoSettings.Threshold;

            return View("~/Plugins/Shipping.Temando/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public IActionResult Configure(TemandoShippingModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return Content("Access denied");

            _temandoSettings.Username = model.Username;
            _temandoSettings.Password = model.Password;
            _temandoSettings.Threshold = model.Threshold;
            //save settings
            _settingService.SaveSetting(_temandoSettings);

            return Json(new { Result = true });
        }

    }
}
