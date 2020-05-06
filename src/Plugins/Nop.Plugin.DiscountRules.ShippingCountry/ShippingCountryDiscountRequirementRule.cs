using System;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core.Plugins;
using Nop.Services.Configuration;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Orders;
using System.Linq;
using System.Collections.Generic;

namespace Nop.Plugin.DiscountRules.ShippingCountry
{
    public partial class ShippingCountryDiscountRequirementRule : BasePlugin, IDiscountRequirementRule
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;

        #endregion

        #region Ctor

        public ShippingCountryDiscountRequirementRule(ISettingService settingService,
            IActionContextAccessor actionContextAccessor,
            IOrderTotalCalculationService orderTotalCalculationService,
            IUrlHelperFactory urlHelperFactory)
        {
            this._settingService = settingService;
            this._actionContextAccessor = actionContextAccessor;
            this._urlHelperFactory = urlHelperFactory;
            this._orderTotalCalculationService = orderTotalCalculationService;
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

            if (request.Customer.ShippingAddress == null)
                return result;

            var shippingCountryId = _settingService.GetSettingByKey<int>($"DiscountRequirement.ShippingCountry-{request.DiscountRequirementId}");

            if (shippingCountryId == 0)
                return result;

            _orderTotalCalculationService.GetShoppingCartSubTotal(request.Customer.ShoppingCartItems.ToList(), true, out decimal _, out List<DiscountForCaching> _, out decimal _, out decimal subTotalWithDiscountBase);

            var isLocal = request.Customer.ShippingAddress.ZipPostalCode == "6430" || request.Customer.ShippingAddress.ZipPostalCode == "6432";
            result.IsValid = request.Customer.ShippingAddress.CountryId == shippingCountryId && subTotalWithDiscountBase >= 50 && isLocal;

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
                new { discountId = discountId, discountRequirementId = discountRequirementId }).TrimStart('/');
        }

        public override void Install()
        {
            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.DiscountRules.ShippingCountry.Fields.SelectCountry", "Select country");
            this.AddOrUpdatePluginLocaleResource("Plugins.DiscountRules.ShippingCountry.Fields.Country", "Shipping country");
            this.AddOrUpdatePluginLocaleResource("Plugins.DiscountRules.ShippingCountry.Fields.Country.Hint", "Select required shipping country.");
            base.Install();
        }

        public override void Uninstall()
        {
            //locales
            this.DeletePluginLocaleResource("Plugins.DiscountRules.ShippingCountry.Fields.SelectCountry");
            this.DeletePluginLocaleResource("Plugins.DiscountRules.ShippingCountry.Fields.Country");
            this.DeletePluginLocaleResource("Plugins.DiscountRules.ShippingCountry.Fields.Country.Hint");
            base.Uninstall();
        }

        #endregion
    }
}