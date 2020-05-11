using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Discounts;
using Nop.Plugin.DiscountRules.ShippingCountry.Models;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.DiscountRules.ShippingCountry.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    public class DiscountRulesShippingCountryController : BasePluginController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly IDiscountService _discountService;
        private readonly ICountryService _countryService;
        private readonly ISettingService _settingService;
        private readonly IPermissionService _permissionService;

        #endregion

        #region Ctor

        public DiscountRulesShippingCountryController(ILocalizationService localizationService,
            IDiscountService discountService, 
            ICountryService countryService,
            ISettingService settingService, 
            IPermissionService permissionService)
        {
            this._localizationService = localizationService;
            this._discountService = discountService;
            this._countryService = countryService;
            this._settingService = settingService;
            this._permissionService = permissionService;
        }

        #endregion

        #region Methods

        public IActionResult Configure(int discountId, int? discountRequirementId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDiscounts))
                return Content("Access denied");

            var discount = _discountService.GetDiscountById(discountId);

            if (discount == null)
                throw new ArgumentException("Discount could not be loaded");

            //check whether the discount requirement exists
            if (discountRequirementId.HasValue && _discountService.GetDiscountRequirementById(discountRequirementId.Value) is null)
                return Content("Failed to load requirement.");

            var shippingCountryId = _settingService.GetSettingByKey<int>($"DiscountRequirement.ShippingCountry-{discountRequirementId ?? 0}");

            var model = new RequirementModel
            {
                RequirementId = discountRequirementId ?? 0,
                DiscountId = discountId,
                CountryId = shippingCountryId
            };

            //countries
            model.AvailableCountries.Add(new SelectListItem { Text = _localizationService.GetResource("Plugins.DiscountRules.ShippingCountry.Fields.SelectCountry"), Value = "0" });

            foreach (var c in _countryService.GetAllCountries(showHidden: true))
                model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString(), Selected = discount != null && c.Id == shippingCountryId });

            //add a prefix
            ViewData.TemplateInfo.HtmlFieldPrefix = $"DiscountRulesShippingCountry{discountRequirementId?.ToString() ?? "0"}";

            return View("~/Plugins/DiscountRules.ShippingCountry/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public IActionResult Configure(int discountId, int? discountRequirementId, int countryId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDiscounts))
                return Content("Access denied");

            var discount = _discountService.GetDiscountById(discountId);

            if (discount == null)
                throw new ArgumentException("Discount could not be loaded");

            //get the discount requirement
            var discountRequirement = _discountService.GetDiscountRequirementById(discountRequirementId.Value);

            //the discount requirement does not exist, so create a new one
            if (discountRequirement == null)
            {
                discountRequirement = new DiscountRequirement
                {
                    DiscountId = discount.Id,
                    DiscountRequirementRuleSystemName = "DiscountRequirement.ShippingCountryIs"
                };

                _discountService.InsertDiscountRequirement(discountRequirement);
            }
            _settingService.SetSetting($"DiscountRequirement.ShippingCountry-{discountRequirement.Id}", countryId);

            return Json(new { Result = true, NewRequirementId = discountRequirement.Id });
        }

        #endregion
    }
}