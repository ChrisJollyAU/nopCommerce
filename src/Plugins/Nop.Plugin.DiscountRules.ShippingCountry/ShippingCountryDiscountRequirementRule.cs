using System;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Services.Configuration;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Orders;
using System.Linq;
using System.Collections.Generic;
using Nop.Core;
using Nop.Core.Domain.Discounts;
using Nop.Services.Common;
using Nop.Services.Plugins;

namespace Nop.Plugin.DiscountRules.ShippingCountry
{
    public partial class ShippingCountryDiscountRequirementRule : BasePlugin, IDiscountRequirementRule
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IAddressService _addressService;
        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;
        private readonly IDiscountService _discountService;
        #endregion

        #region Ctor

        public ShippingCountryDiscountRequirementRule(ISettingService settingService,
            IActionContextAccessor actionContextAccessor,
            IOrderTotalCalculationService orderTotalCalculationService,
            IShoppingCartService shoppingCartService,
            IAddressService addressService,
            IWebHelper webHelper,
            ILocalizationService localizationService,
            IDiscountService discountService,
            IUrlHelperFactory urlHelperFactory)
        {
            this._settingService = settingService;
            this._actionContextAccessor = actionContextAccessor;
            this._urlHelperFactory = urlHelperFactory;
            this._orderTotalCalculationService = orderTotalCalculationService;
            _shoppingCartService = shoppingCartService;
            _addressService = addressService;
            _webHelper = webHelper;
            _localizationService = localizationService;
            _discountService = discountService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Check discount requirement
        /// </summary>
        /// <param name="request">Object that contains all information required to check the requirement (Current customer, discount, etc)</param>
        /// <returns>Result</returns>
        public DiscountRequirementValidationResult CheckRequirement(DiscountRequirementValidationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            //invalid by default
            var result = new DiscountRequirementValidationResult();

            if (request.Customer == null)
                return result;

            if (request.Customer.ShippingAddressId == null)
                return result;

            var shippingCountryId = _settingService.GetSettingByKey<int>($"DiscountRequirement.ShippingCountry-{request.DiscountRequirementId}");

            if (shippingCountryId == 0)
                return result;

            var cart = _shoppingCartService.GetShoppingCart(request.Customer);
            _orderTotalCalculationService.GetShoppingCartSubTotal(cart.ToList(), true, out var _, out var _, out var _, out var subTotalWithDiscountBase);
            var shipaddress = _addressService.GetAddressById(request.Customer.ShippingAddressId.Value);
            var isLocal = shipaddress.ZipPostalCode == "6430" || shipaddress.ZipPostalCode == "6432";
            result.IsValid = shipaddress.CountryId == shippingCountryId && subTotalWithDiscountBase >= 50 && isLocal;

            return result;
        }

        /// <summary>
        /// Get URL for rule configuration
        /// </summary>
        /// <param name="discountId">Discount identifier</param>
        /// <param name="discountRequirementId">Discount requirement identifier (if editing)</param>
        /// <returns>URL</returns>
        public string GetConfigurationUrl(int discountId, int? discountRequirementId)
        {
            var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);

            return urlHelper.Action("Configure", "DiscountRulesShippingCountry",
                new { discountId = discountId, discountRequirementId = discountRequirementId }, _webHelper.CurrentRequestProtocol);
        }

        public override void Install()
        {
            //locales
            _localizationService.AddPluginLocaleResource(new Dictionary<string, string>
            {
                ["Plugins.DiscountRules.ShippingCountry.Fields.SelectCountry"] = "Select country",
                ["Plugins.DiscountRules.ShippingCountry.Fields.Country"] = "Shipping country",
                ["Plugins.DiscountRules.ShippingCountry.Fields.Country.Hint"] = "Select required shipping country."
            });

            base.Install();
        }

        public override void Uninstall()
        {
            //discount requirements
            var discountRequirements = _discountService.GetAllDiscountRequirements()
                .Where(discountRequirement => discountRequirement.DiscountRequirementRuleSystemName == "DiscountRequirement.ShippingCountryIs");
            foreach (var discountRequirement in discountRequirements)
            {
                _discountService.DeleteDiscountRequirement(discountRequirement, false);
            }
            //locales
            _localizationService.DeletePluginLocaleResources("Plugins.DiscountRules.ShippingCountry");
            base.Uninstall();
        }

        #endregion
    }
}