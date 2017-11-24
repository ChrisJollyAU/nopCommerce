using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Nop.Plugin.Payments.ZipMoney.Models;
using Nop.Services.Configuration;
using Nop.Services.Payments;
using Nop.Services.Orders;
using Nop.Core;
using Nop.Core.Domain.Payments;
using Nop.Services.Localization;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Services.Customers;
using ZipMoneySDK.Models;
using Nop.Web.Framework;
using Nop.Services.Security;
using Nop.Services.Stores;
using ZipMoneySDK;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Core.Domain.Directory;
using Nop.Services.Affiliates;
using Nop.Services.Catalog;
using Nop.Services.Tax;

namespace Nop.Plugin.Payments.ZipMoney.Controllers
{
    public class PaymentZipMoneyController : BasePaymentController
    {
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IWebHelper _webHelper;
        private readonly ZipMoneyPaymentSettings _zipMoneyPaymentSettings;
        private readonly PaymentSettings _paymentSettings;
        private readonly ILocalizationService _localizationService;
        private readonly IPermissionService _permissionService;
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly IStoreContext _storeContext;
        private readonly ICustomerService _customerService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly ILanguageService _languageService;
        private readonly IAffiliateService _affiliateService;
        private readonly OrderSettings _orderSettings;
        private readonly TaxSettings _taxSettings;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IProductService _productService;
        private readonly ICheckoutAttributeFormatter _checkoutAttributeFormatter;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly ShippingSettings _shippingSettings;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ITaxService _taxService;

        public PaymentZipMoneyController(ISettingService settingService,
            IWorkContext workContext,
            IStoreService storeService,
            IPaymentService paymentService, IOrderService orderService, 
            IOrderProcessingService orderProcessingService, 
            IWebHelper webHelper,
            ZipMoneyPaymentSettings zipMoneyPaymentSettings,
            IStoreContext storeContext,
            PaymentSettings paymentSettings,
            ShoppingCartService shoppingCartService,
            IPermissionService permissionService,
            ICustomerService customerService,
            ILocalizationService localizationService)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._webHelper = webHelper;
            this._zipMoneyPaymentSettings = zipMoneyPaymentSettings;
            this._paymentSettings = paymentSettings;
            this._localizationService = localizationService;
            this._shoppingCartService = shoppingCartService;
            this._permissionService = permissionService;
            this._customerService = customerService;
            this._storeContext = storeContext;
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            //whether user has the authority
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var zipMoneyPaymentSettings = _settingService.LoadSetting<ZipMoneyPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                UseSandbox = zipMoneyPaymentSettings.UseSandbox,
                SandboxAPIKey = zipMoneyPaymentSettings.SandboxAPIKey,
                SandboxPublicKey = zipMoneyPaymentSettings.SandboxPublicKey,
                ProductionAPIKey = zipMoneyPaymentSettings.ProductionAPIKey,
                ProductionPublicKey = zipMoneyPaymentSettings.ProductionPublicKey,
                ActiveStoreScopeConfiguration = storeScope
            };
            if (storeScope > 0)
            {
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(zipMoneyPaymentSettings, x => x.UseSandbox, storeScope);
                model.SandboxAPIKey_OverrideForStore = _settingService.SettingExists(zipMoneyPaymentSettings, x => x.SandboxAPIKey, storeScope);
                model.SandboxPublicKey_OverrideForStore = _settingService.SettingExists(zipMoneyPaymentSettings, x => x.SandboxPublicKey, storeScope);
                model.ProductionAPIKey_OverrideForStore = _settingService.SettingExists(zipMoneyPaymentSettings, x => x.ProductionAPIKey, storeScope);
                model.ProductionPublicKey_OverrideForStore = _settingService.SettingExists(zipMoneyPaymentSettings, x => x.ProductionPublicKey, storeScope);
            }

            return View("~/Plugins/Payments.ZipMoney/Views/Configure.cshtml", model);
        }

        [AuthorizeAdmin]
        [AdminAntiForgery]
        [Area(AreaNames.Admin)]
        public IActionResult Configure(ConfigurationModel model)
        {
            //whether user has the authority
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var zipMoneyPaymentSettings = _settingService.LoadSetting<ZipMoneyPaymentSettings>(storeScope);

            //save settings
            zipMoneyPaymentSettings.UseSandbox = model.UseSandbox;
            zipMoneyPaymentSettings.SandboxAPIKey = model.SandboxAPIKey;
            zipMoneyPaymentSettings.SandboxPublicKey = model.SandboxPublicKey;
            zipMoneyPaymentSettings.ProductionAPIKey = model.ProductionAPIKey;
            zipMoneyPaymentSettings.ProductionPublicKey = model.ProductionPublicKey;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(zipMoneyPaymentSettings, x => x.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(zipMoneyPaymentSettings, x => x.SandboxAPIKey, model.SandboxAPIKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(zipMoneyPaymentSettings, x => x.SandboxPublicKey, model.SandboxPublicKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(zipMoneyPaymentSettings, x => x.ProductionAPIKey, model.ProductionAPIKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(zipMoneyPaymentSettings, x => x.ProductionPublicKey, model.ProductionPublicKey_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        public IActionResult CancelOrder()
        {
            var order = _orderService.SearchOrders(storeId: _storeContext.CurrentStore.Id,
                customerId: _workContext.CurrentCustomer.Id, pageSize: 1).FirstOrDefault();
            if (order != null)
                return RedirectToRoute("OrderDetails", new { orderId = order.Id });

            return RedirectToRoute("HomePage");
        }

        public async Task<string> ZipCheckout()
        {
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var zipMoneyPaymentSettings = _settingService.LoadSetting<ZipMoneyPaymentSettings>(storeScope);
            ZipCheckout zipCheckout = new ZipCheckout();
            string apikey = zipMoneyPaymentSettings.UseSandbox
                ? zipMoneyPaymentSettings.SandboxAPIKey
                : zipMoneyPaymentSettings.ProductionAPIKey;
            ZipMoneyProcessor zm = new ZipMoneyProcessor(apikey, true);
            ZipCheckoutResponse zcr = await zm.CreateCheckout(zipCheckout);
            return JsonConvert.SerializeObject(zcr);
        }

        public IActionResult ZipRedirect(string result,string checkoutid)
        {
            decimal amount = 0;
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var zipMoneyPaymentSettings = _settingService.LoadSetting<ZipMoneyPaymentSettings>(storeScope);
            ZipCharge zipCharge = new ZipCharge();
            string apikey = zipMoneyPaymentSettings.UseSandbox
                ? zipMoneyPaymentSettings.SandboxAPIKey
                : zipMoneyPaymentSettings.ProductionAPIKey;
            zipCharge.authority.type = "checkout_id";
            zipCharge.authority.value = checkoutid;
            zipCharge.capture = false;
            zipCharge.amount = amount;
            ZipMoneyProcessor zm = new ZipMoneyProcessor(apikey, true);
            var response = zm.CreateCharge(zipCharge).Result;
            return null;
        }

        void GetOrderDetails()
        {
            int StoreId = _storeContext.CurrentStore.Id;
            //customer
            var Customer = _customerService.GetCustomerById(_workContext.CurrentCustomer.Id);

            //affiliate
            var affiliate = _affiliateService.GetAffiliateById(Customer.AffiliateId);
            if (affiliate != null && affiliate.Active && !affiliate.Deleted)
                details.AffiliateId = affiliate.Id;

            //customer currency
            var currencyTmp = _currencyService.GetCurrencyById(
                Customer.GetAttribute<int>(SystemCustomerAttributeNames.CurrencyId, StoreId));
            var customerCurrency = (currencyTmp != null && currencyTmp.Published) ? currencyTmp : _workContext.WorkingCurrency;
            var primaryStoreCurrency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
            var CustomerCurrencyCode = customerCurrency.CurrencyCode;
            var CustomerCurrencyRate = customerCurrency.Rate / primaryStoreCurrency.Rate;

            //customer language
            var CustomerLanguage = _languageService.GetLanguageById(
                Customer.GetAttribute<int>(SystemCustomerAttributeNames.LanguageId, StoreId));
            if (CustomerLanguage == null || !CustomerLanguage.Published)
                CustomerLanguage = _workContext.WorkingLanguage;

            //billing address
            if (Customer.BillingAddress == null)
                throw new NopException("Billing address is not provided");

            if (!CommonHelper.IsValidEmail(Customer.BillingAddress.Email))
                throw new NopException("Email is not valid");

            var BillingAddress = (Address)Customer.BillingAddress.Clone();
            if (BillingAddress.Country != null && !BillingAddress.Country.AllowsBilling)
                throw new NopException($"Country '{BillingAddress.Country.Name}' is not allowed for billing");

            //checkout attributes
            var CheckoutAttributesXml = Customer.GetAttribute<string>(SystemCustomerAttributeNames.CheckoutAttributes, StoreId);
            var CheckoutAttributeDescription = _checkoutAttributeFormatter.FormatAttributes(CheckoutAttributesXml, Customer);

            //load shopping cart
            var Cart = Customer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                .LimitPerStore(StoreId).ToList();

            if (!Cart.Any())
                throw new NopException("Cart is empty");

            //validate the entire shopping cart
            var warnings = _shoppingCartService.GetShoppingCartWarnings(Cart, CheckoutAttributesXml, true);
            if (warnings.Any())
                throw new NopException(warnings.Aggregate(string.Empty, (current, next) => $"{current}{next};"));

            //validate individual cart items
            foreach (var sci in Cart)
            {
                var sciWarnings = _shoppingCartService.GetShoppingCartItemWarnings(Customer,
                    sci.ShoppingCartType, sci.Product, StoreId, sci.AttributesXml,
                    sci.CustomerEnteredPrice, sci.RentalStartDateUtc, sci.RentalEndDateUtc, sci.Quantity, false);
                if (sciWarnings.Any())
                    throw new NopException(sciWarnings.Aggregate(string.Empty, (current, next) => $"{current}{next};"));
            }

            //min totals validation
            if (!ValidateMinOrderSubtotalAmount(Cart))
            {
                var minOrderSubtotalAmount = _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderSubtotalAmount, _workContext.WorkingCurrency);
                throw new NopException(string.Format(_localizationService.GetResource("Checkout.MinOrderSubtotalAmount"),
                    _priceFormatter.FormatPrice(minOrderSubtotalAmount, true, false)));
            }

            if (!ValidateMinOrderTotalAmount(Cart))
            {
                var minOrderTotalAmount = _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderTotalAmount, _workContext.WorkingCurrency);
                throw new NopException(string.Format(_localizationService.GetResource("Checkout.MinOrderTotalAmount"),
                    _priceFormatter.FormatPrice(minOrderTotalAmount, true, false)));
            }
            TaxDisplayType CustomerTaxDisplayType;
            //tax display type
            if (_taxSettings.AllowCustomersToSelectTaxDisplayType)
                CustomerTaxDisplayType = (TaxDisplayType)Customer.GetAttribute<int>(SystemCustomerAttributeNames.TaxDisplayTypeId, StoreId);
            else
                CustomerTaxDisplayType = _taxSettings.TaxDisplayType;

            //sub total (incl tax)
            _orderTotalCalculationService.GetShoppingCartSubTotal(Cart, true, out decimal orderSubTotalDiscountAmount, out List<DiscountForCaching> orderSubTotalAppliedDiscounts, out decimal subTotalWithoutDiscountBase, out decimal _);
            var OrderSubTotalInclTax = subTotalWithoutDiscountBase;
            var OrderSubTotalDiscountInclTax = orderSubTotalDiscountAmount;

            //discount history
            foreach (var disc in orderSubTotalAppliedDiscounts)
                if (!details.AppliedDiscounts.ContainsDiscount(disc))
                    details.AppliedDiscounts.Add(disc);

            //sub total (excl tax)
            _orderTotalCalculationService.GetShoppingCartSubTotal(Cart, false, out orderSubTotalDiscountAmount,
                out orderSubTotalAppliedDiscounts, out subTotalWithoutDiscountBase, out _);
            var OrderSubTotalExclTax = subTotalWithoutDiscountBase;
            var OrderSubTotalDiscountExclTax = orderSubTotalDiscountAmount;

            //shipping info
            if (Cart.RequiresShipping(_productService, _productAttributeParser))
            {
                var pickupPoint = Customer.GetAttribute<PickupPoint>(SystemCustomerAttributeNames.SelectedPickupPoint, StoreId);
                if (_shippingSettings.AllowPickUpInStore && pickupPoint != null)
                {
                    var country = _countryService.GetCountryByTwoLetterIsoCode(pickupPoint.CountryCode);
                    var state = _stateProvinceService.GetStateProvinceByAbbreviation(pickupPoint.StateAbbreviation, country?.Id);

                    PickUpInStore = true;
                    PickupAddress = new Address
                    {
                        Address1 = pickupPoint.Address,
                        City = pickupPoint.City,
                        Country = country,
                        StateProvince = state,
                        ZipPostalCode = pickupPoint.ZipPostalCode,
                        CreatedOnUtc = DateTime.UtcNow,
                    };
                }
                else
                {
                    if (Customer.ShippingAddress == null)
                        throw new NopException("Shipping address is not provided");

                    if (!CommonHelper.IsValidEmail(Customer.ShippingAddress.Email))
                        throw new NopException("Email is not valid");

                    //clone shipping address
                    ShippingAddress = (Address)Customer.ShippingAddress.Clone();
                    if (ShippingAddress.Country != null && !ShippingAddress.Country.AllowsShipping)
                        throw new NopException($"Country '{ShippingAddress.Country.Name}' is not allowed for shipping");
                }

                var shippingOption = Customer.GetAttribute<ShippingOption>(SystemCustomerAttributeNames.SelectedShippingOption, processPaymentRequest.StoreId);
                if (shippingOption != null)
                {
                    ShippingMethodName = shippingOption.Name;
                    ShippingRateComputationMethodSystemName = shippingOption.ShippingRateComputationMethodSystemName;
                }

                ShippingStatus = ShippingStatus.NotYetShipped;
            }
            else
                ShippingStatus = ShippingStatus.ShippingNotRequired;

            //shipping total
            var orderShippingTotalInclTax = _orderTotalCalculationService.GetShoppingCartShippingTotal(Cart, true, out decimal _, out List<DiscountForCaching> shippingTotalDiscounts);
            var orderShippingTotalExclTax = _orderTotalCalculationService.GetShoppingCartShippingTotal(Cart, false);
            if (!orderShippingTotalInclTax.HasValue || !orderShippingTotalExclTax.HasValue)
                throw new NopException("Shipping total couldn't be calculated");

            var OrderShippingTotalInclTax = orderShippingTotalInclTax.Value;
            var OrderShippingTotalExclTax = orderShippingTotalExclTax.Value;

            foreach (var disc in shippingTotalDiscounts)
                if (!details.AppliedDiscounts.ContainsDiscount(disc))
                    details.AppliedDiscounts.Add(disc);

            //payment total
            var paymentAdditionalFee = _paymentService.GetAdditionalHandlingFee(Cart, processPaymentRequest.PaymentMethodSystemName);
            var PaymentAdditionalFeeInclTax = _taxService.GetPaymentMethodAdditionalFee(paymentAdditionalFee, true, Customer);
            var PaymentAdditionalFeeExclTax = _taxService.GetPaymentMethodAdditionalFee(paymentAdditionalFee, false, Customer);

            //tax amount
            var OrderTaxTotal = _orderTotalCalculationService.GetTaxTotal(Cart, out SortedDictionary<decimal, decimal> taxRatesDictionary);

            //VAT number
            var customerVatStatus = (VatNumberStatus)Customer.GetAttribute<int>(SystemCustomerAttributeNames.VatNumberStatusId);
            if (_taxSettings.EuVatEnabled && customerVatStatus == VatNumberStatus.Valid)
                VatNumber = Customer.GetAttribute<string>(SystemCustomerAttributeNames.VatNumber);

            //tax rates
            TaxRates = taxRatesDictionary.Aggregate(string.Empty, (current, next) =>
                $"{current}{next.Key.ToString(CultureInfo.InvariantCulture)}:{next.Value.ToString(CultureInfo.InvariantCulture)};   ");

            //order total (and applied discounts, gift cards, reward points)
            var orderTotal = _orderTotalCalculationService.GetShoppingCartTotal(Cart, out decimal orderDiscountAmount, out List<DiscountForCaching> orderAppliedDiscounts, out List<AppliedGiftCard> appliedGiftCards, out int redeemedRewardPoints, out decimal redeemedRewardPointsAmount);
            if (!orderTotal.HasValue)
                throw new NopException("Order total couldn't be calculated");

            OrderDiscountAmount = orderDiscountAmount;
            RedeemedRewardPoints = redeemedRewardPoints;
            RedeemedRewardPointsAmount = redeemedRewardPointsAmount;
            AppliedGiftCards = appliedGiftCards;
            OrderTotal = orderTotal.Value;

            //discount history
            foreach (var disc in orderAppliedDiscounts)
                if (!details.AppliedDiscounts.ContainsDiscount(disc))
                    details.AppliedDiscounts.Add(disc);


            //recurring or standard shopping cart?
            IsRecurringShoppingCart = Cart.IsRecurring();
            if (IsRecurringShoppingCart)
            {
                var recurringCyclesError = Cart.GetRecurringCycleInfo(_localizationService, out int recurringCycleLength, out RecurringProductCyclePeriod recurringCyclePeriod, out int recurringTotalCycles);
                if (!string.IsNullOrEmpty(recurringCyclesError))
                    throw new NopException(recurringCyclesError);

                RecurringCycleLength = recurringCycleLength;
                RecurringCyclePeriod = recurringCyclePeriod;
                RecurringTotalCycles = recurringTotalCycles;
            }
        }
    }
}