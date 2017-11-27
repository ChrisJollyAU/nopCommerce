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
using Nop.Core.Domain.Localization;
using Nop.Services.Affiliates;
using Nop.Services.Catalog;
using Nop.Services.Media;
using Nop.Services.Tax;

namespace Nop.Plugin.Payments.ZipMoney.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
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
            ICurrencyService currencyService,
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
            _currencyService = currencyService;
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
                model.UseSandbox_OverrideForStore =
                    _settingService.SettingExists(zipMoneyPaymentSettings, x => x.UseSandbox, storeScope);
                model.SandboxAPIKey_OverrideForStore =
                    _settingService.SettingExists(zipMoneyPaymentSettings, x => x.SandboxAPIKey, storeScope);
                model.SandboxPublicKey_OverrideForStore =
                    _settingService.SettingExists(zipMoneyPaymentSettings, x => x.SandboxPublicKey, storeScope);
                model.ProductionAPIKey_OverrideForStore =
                    _settingService.SettingExists(zipMoneyPaymentSettings, x => x.ProductionAPIKey, storeScope);
                model.ProductionPublicKey_OverrideForStore = _settingService.SettingExists(zipMoneyPaymentSettings,
                    x => x.ProductionPublicKey, storeScope);
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
            _settingService.SaveSettingOverridablePerStore(zipMoneyPaymentSettings, x => x.UseSandbox,
                model.UseSandbox_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(zipMoneyPaymentSettings, x => x.SandboxAPIKey,
                model.SandboxAPIKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(zipMoneyPaymentSettings, x => x.SandboxPublicKey,
                model.SandboxPublicKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(zipMoneyPaymentSettings, x => x.ProductionAPIKey,
                model.ProductionAPIKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(zipMoneyPaymentSettings, x => x.ProductionPublicKey,
                model.ProductionPublicKey_OverrideForStore, storeScope, false);

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
                return RedirectToRoute("OrderDetails", new {orderId = order.Id});

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
            var details = GetOrderDetails();
            zipCheckout.shopper.billing_address.first_name = details.BillingAddress.FirstName;
            zipCheckout.shopper.billing_address.last_name = details.BillingAddress.LastName;
            zipCheckout.shopper.billing_address.line1 = details.BillingAddress.Address1;
            zipCheckout.shopper.billing_address.line2 = details.BillingAddress.Address2;
            zipCheckout.shopper.billing_address.city = details.BillingAddress.City;
            zipCheckout.shopper.billing_address.country = details.BillingAddress.Country.TwoLetterIsoCode;
            zipCheckout.shopper.billing_address.postal_code = details.BillingAddress.ZipPostalCode;
            zipCheckout.shopper.billing_address.state = details.BillingAddress.StateProvince.Abbreviation;
            zipCheckout.shopper.email = details.BillingAddress.Email;
            zipCheckout.shopper.phone = details.BillingAddress.PhoneNumber.Replace(" ", "");
            zipCheckout.shopper.first_name = details.BillingAddress.FirstName;
            zipCheckout.shopper.last_name = details.BillingAddress.LastName;
            zipCheckout.shopper.statistics.account_created = details.Customer.CreatedOnUtc;
            zipCheckout.shopper.statistics.currency = "AUD";
            if (details.Customer.LastLoginDateUtc != null)
                zipCheckout.shopper.statistics.last_login = details.Customer.LastLoginDateUtc.Value;
            zipCheckout.order.order_amount = details.OrderTotal;
            zipCheckout.order.shipping.pickup = details.PickUpInStore;
            zipCheckout.order.shipping.address.line1 = details.ShippingAddress.Address1;
            zipCheckout.order.shipping.address.line2 = details.ShippingAddress.Address2;
            zipCheckout.order.shipping.address.first_name = details.ShippingAddress.FirstName;
            zipCheckout.order.shipping.address.last_name = details.ShippingAddress.LastName;
            zipCheckout.order.shipping.address.city = details.ShippingAddress.City;
            zipCheckout.order.shipping.address.country = details.ShippingAddress.Country.TwoLetterIsoCode;
            zipCheckout.order.shipping.address.state = details.ShippingAddress.StateProvince.Name;
            zipCheckout.order.shipping.address.postal_code = details.ShippingAddress.ZipPostalCode;
            foreach (var item in details.Cart)
            {
                ZipOrderItem zipOrderItem = new ZipOrderItem
                {
                    amount = item.Product.Price * item.Quantity,
                    name = item.Product.Name,
                    quantity = item.Quantity,
                    type = "sku",
                    reference = item.Product.Sku
                };
                var url = "https://www.freerangesupplies.com.au/content/images/";
                string fname = "" + item.Product.ProductPictures.First().Picture.Id;
                switch (item.Product.ProductPictures.First().Picture.MimeType)
                {
                    case "image/png":
                        while (fname.Length < 7)
                            fname = fname.Insert(0, "0");
                        fname += "_0.png";
                        break;
                    case "image/gif":
                        while (fname.Length < 7)
                            fname = fname.Insert(0, "0");
                        fname += "_0.gif";
                        break;
                    case "image/jpeg":
                    default:
                        while (fname.Length < 7)
                            fname = fname.Insert(0, "0");
                        fname += "_0.jpeg";
                        break;
                }
                zipOrderItem.image_uri = url + fname;
                zipCheckout.order.items.Add(zipOrderItem);
            }
            ZipOrderItem shipItem = new ZipOrderItem();
            ZipOrderItem taxItem = new ZipOrderItem();
            ZipOrderItem discountItem = new ZipOrderItem();
            shipItem.amount = details.OrderShippingTotalInclTax;
            shipItem.name = "Shipping";
            shipItem.quantity = 1;
            shipItem.type = "shipping";
            shipItem.reference = "shipping";
            zipCheckout.order.items.Add(shipItem);

            discountItem.amount = details.OrderDiscountAmount;
            discountItem.name = "discount";
            discountItem.quantity = 1;
            discountItem.type = "discount";
            discountItem.reference = "discount";
            zipCheckout.order.items.Add(discountItem);

            zipCheckout.config.redirect_uri = "~/Payment.ZipMoney/ZipRedirect";

            ZipMoneyProcessor zm = new ZipMoneyProcessor(apikey, true);
            ZipCheckoutResponse zcr = await zm.CreateCheckout(zipCheckout);
            return JsonConvert.SerializeObject(zcr);
        }

        public IActionResult ZipRedirect(string result, string checkoutid)
        {
            if (result.ToLowerInvariant().Equals("approved"))
            {
                var details = GetOrderDetails();
                decimal amount = details.OrderTotal;
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
                zipCharge.currency = "AUD";
                ZipMoneyProcessor zm = new ZipMoneyProcessor(apikey, true);
                var response = zm.CreateCharge(zipCharge).Result;
                return RedirectToPage("~/confirmorder");
            }
            return RedirectToPage("~/cart");
        }

        PlaceOrderContainer GetOrderDetails()
        {
            PlaceOrderContainer details = new PlaceOrderContainer();
            int StoreId = _storeContext.CurrentStore.Id;
            //customer
            details.Customer = _customerService.GetCustomerById(_workContext.CurrentCustomer.Id);

            //affiliate
            var affiliate = _affiliateService.GetAffiliateById(details.Customer.AffiliateId);
            if (affiliate != null && affiliate.Active && !affiliate.Deleted)
                details.AffiliateId = affiliate.Id;

            //customer currency
            var currencyTmp = _currencyService.GetCurrencyById(
                details.Customer.GetAttribute<int>(SystemCustomerAttributeNames.CurrencyId, StoreId));
            var customerCurrency = (currencyTmp != null && currencyTmp.Published)
                ? currencyTmp
                : _workContext.WorkingCurrency;
            var primaryStoreCurrency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
            details.CustomerCurrencyCode = customerCurrency.CurrencyCode;
            details.CustomerCurrencyRate = customerCurrency.Rate / primaryStoreCurrency.Rate;

            //customer language
            details.CustomerLanguage = _languageService.GetLanguageById(
                details.Customer.GetAttribute<int>(SystemCustomerAttributeNames.LanguageId, StoreId));
            if (details.CustomerLanguage == null || !details.CustomerLanguage.Published)
                details.CustomerLanguage = _workContext.WorkingLanguage;

            //billing address
            if (details.Customer.BillingAddress == null)
                throw new NopException("Billing address is not provided");

            if (!CommonHelper.IsValidEmail(details.Customer.BillingAddress.Email))
                throw new NopException("Email is not valid");

            details.BillingAddress = (Address)details.Customer.BillingAddress.Clone();
            if (details.BillingAddress.Country != null && !details.BillingAddress.Country.AllowsBilling)
                throw new NopException($"Country '{details.BillingAddress.Country.Name}' is not allowed for billing");

            //checkout attributes
            details.CheckoutAttributesXml =
                details.Customer.GetAttribute<string>(SystemCustomerAttributeNames.CheckoutAttributes, StoreId);
            details.CheckoutAttributeDescription =
                _checkoutAttributeFormatter.FormatAttributes(details.CheckoutAttributesXml, details.Customer);

            //load shopping cart
            details.Cart = details.Customer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                .LimitPerStore(StoreId).ToList();

            if (!details.Cart.Any())
                throw new NopException("Cart is empty");

            //validate the entire shopping cart
            var warnings = _shoppingCartService.GetShoppingCartWarnings(details.Cart, details.CheckoutAttributesXml, true);
            if (warnings.Any())
                throw new NopException(warnings.Aggregate(string.Empty, (current, next) => $"{current}{next};"));

            //validate individual cart items
            foreach (var sci in details.Cart)
            {
                var sciWarnings = _shoppingCartService.GetShoppingCartItemWarnings(details.Customer,
                    sci.ShoppingCartType, sci.Product, StoreId, sci.AttributesXml,
                    sci.CustomerEnteredPrice, sci.RentalStartDateUtc, sci.RentalEndDateUtc, sci.Quantity, false);
                if (sciWarnings.Any())
                    throw new NopException(sciWarnings.Aggregate(string.Empty, (current, next) => $"{current}{next};"));
            }

            //min totals validation
            if (!ValidateMinOrderSubtotalAmount(details.Cart))
            {
                var minOrderSubtotalAmount =
                    _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderSubtotalAmount,
                        _workContext.WorkingCurrency);
                throw new NopException(string.Format(
                    _localizationService.GetResource("Checkout.MinOrderSubtotalAmount"),
                    _priceFormatter.FormatPrice(minOrderSubtotalAmount, true, false)));
            }

            if (!ValidateMinOrderTotalAmount(details.Cart))
            {
                var minOrderTotalAmount =
                    _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderTotalAmount,
                        _workContext.WorkingCurrency);
                throw new NopException(string.Format(_localizationService.GetResource("Checkout.MinOrderTotalAmount"),
                    _priceFormatter.FormatPrice(minOrderTotalAmount, true, false)));
            }

            //tax display type
            if (_taxSettings.AllowCustomersToSelectTaxDisplayType)
                details.CustomerTaxDisplayType =
                    (TaxDisplayType)details.Customer.GetAttribute<int>(SystemCustomerAttributeNames.TaxDisplayTypeId, StoreId);
            else
                details.CustomerTaxDisplayType = _taxSettings.TaxDisplayType;

            //sub total (incl tax)
            _orderTotalCalculationService.GetShoppingCartSubTotal(details.Cart, true, out decimal orderSubTotalDiscountAmount,
                out List<DiscountForCaching> orderSubTotalAppliedDiscounts, out decimal subTotalWithoutDiscountBase,
                out decimal _);
            details.OrderSubTotalInclTax = subTotalWithoutDiscountBase;
            details.OrderSubTotalDiscountInclTax = orderSubTotalDiscountAmount;

            //discount history
            foreach (var disc in orderSubTotalAppliedDiscounts)
                if (!details.AppliedDiscounts.ContainsDiscount(disc))
                    details.AppliedDiscounts.Add(disc);

            //sub total (excl tax)
            _orderTotalCalculationService.GetShoppingCartSubTotal(details.Cart, false, out orderSubTotalDiscountAmount,
                out orderSubTotalAppliedDiscounts, out subTotalWithoutDiscountBase, out _);
            details.OrderSubTotalExclTax = subTotalWithoutDiscountBase;
            details.OrderSubTotalDiscountExclTax = orderSubTotalDiscountAmount;

            //shipping info
            if (details.Cart.RequiresShipping(_productService, _productAttributeParser))
            {
                var pickupPoint =
                    details.Customer.GetAttribute<PickupPoint>(SystemCustomerAttributeNames.SelectedPickupPoint, StoreId);
                if (_shippingSettings.AllowPickUpInStore && pickupPoint != null)
                {
                    var country = _countryService.GetCountryByTwoLetterIsoCode(pickupPoint.CountryCode);
                    var state = _stateProvinceService.GetStateProvinceByAbbreviation(pickupPoint.StateAbbreviation,
                        country?.Id);

                    details.PickUpInStore = true;
                    details.PickupAddress = new Address
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
                    if (details.Customer.ShippingAddress == null)
                        throw new NopException("Shipping address is not provided");

                    if (!CommonHelper.IsValidEmail(details.Customer.ShippingAddress.Email))
                        throw new NopException("Email is not valid");

                    //clone shipping address
                    details.ShippingAddress = (Address)details.Customer.ShippingAddress.Clone();
                    if (details.ShippingAddress.Country != null && !details.ShippingAddress.Country.AllowsShipping)
                        throw new NopException($"Country '{details.ShippingAddress.Country.Name}' is not allowed for shipping");
                }

                var shippingOption =
                    details.Customer.GetAttribute<ShippingOption>(SystemCustomerAttributeNames.SelectedShippingOption,
                        StoreId);
                if (shippingOption != null)
                {
                    details.ShippingMethodName = shippingOption.Name;
                    details.ShippingRateComputationMethodSystemName = shippingOption.ShippingRateComputationMethodSystemName;
                }

                details.ShippingStatus = ShippingStatus.NotYetShipped;
            }
            else
                details.ShippingStatus = ShippingStatus.ShippingNotRequired;

            //shipping total
            var orderShippingTotalInclTax = _orderTotalCalculationService.GetShoppingCartShippingTotal(details.Cart, true,
                out decimal _, out List<DiscountForCaching> shippingTotalDiscounts);
            var orderShippingTotalExclTax = _orderTotalCalculationService.GetShoppingCartShippingTotal(details.Cart, false);
            if (!orderShippingTotalInclTax.HasValue || !orderShippingTotalExclTax.HasValue)
                throw new NopException("Shipping total couldn't be calculated");

            details.OrderShippingTotalInclTax = orderShippingTotalInclTax.Value;
            details.OrderShippingTotalExclTax = orderShippingTotalExclTax.Value;

            foreach (var disc in shippingTotalDiscounts)
                if (!details.AppliedDiscounts.ContainsDiscount(disc))
                    details.AppliedDiscounts.Add(disc);

            //payment total
            var paymentAdditionalFee =
                _paymentService.GetAdditionalHandlingFee(details.Cart, "Payment.ZipMoney");
            details.PaymentAdditionalFeeInclTax =
                _taxService.GetPaymentMethodAdditionalFee(paymentAdditionalFee, true, details.Customer);
            details.PaymentAdditionalFeeExclTax =
                _taxService.GetPaymentMethodAdditionalFee(paymentAdditionalFee, false, details.Customer);

            //tax amount
            details.OrderTaxTotal =
                _orderTotalCalculationService.GetTaxTotal(details.Cart,
                    out SortedDictionary<decimal, decimal> taxRatesDictionary);

            //VAT number
            var customerVatStatus =
                (VatNumberStatus)details.Customer.GetAttribute<int>(SystemCustomerAttributeNames.VatNumberStatusId);
            if (_taxSettings.EuVatEnabled && customerVatStatus == VatNumberStatus.Valid)
                details.VatNumber = details.Customer.GetAttribute<string>(SystemCustomerAttributeNames.VatNumber);

            //tax rates
            details.TaxRates = taxRatesDictionary.Aggregate(string.Empty, (current, next) =>
                $"{current}{next.Key.ToString(CultureInfo.InvariantCulture)}:{next.Value.ToString(CultureInfo.InvariantCulture)};   ");

            //order total (and applied discounts, gift cards, reward points)
            var orderTotal = _orderTotalCalculationService.GetShoppingCartTotal(details.Cart, out decimal orderDiscountAmount,
                out List<DiscountForCaching> orderAppliedDiscounts, out List<AppliedGiftCard> appliedGiftCards,
                out int redeemedRewardPoints, out decimal redeemedRewardPointsAmount);
            if (!orderTotal.HasValue)
                throw new NopException("Order total couldn't be calculated");

            details.OrderDiscountAmount = orderDiscountAmount;
            details.RedeemedRewardPoints = redeemedRewardPoints;
            details.RedeemedRewardPointsAmount = redeemedRewardPointsAmount;
            details.AppliedGiftCards = appliedGiftCards;
            details.OrderTotal = orderTotal.Value;

            //discount history
            foreach (var disc in orderAppliedDiscounts)
                if (!details.AppliedDiscounts.ContainsDiscount(disc))
                    details.AppliedDiscounts.Add(disc);


            //recurring or standard shopping cart?
            details.IsRecurringShoppingCart = details.Cart.IsRecurring();
            if (details.IsRecurringShoppingCart)
            {
                var recurringCyclesError = details.Cart.GetRecurringCycleInfo(_localizationService,
                    out int recurringCycleLength, out RecurringProductCyclePeriod recurringCyclePeriod,
                    out int recurringTotalCycles);
                if (!string.IsNullOrEmpty(recurringCyclesError))
                    throw new NopException(recurringCyclesError);

                //RecurringCycleLength = recurringCycleLength;
                //RecurringCyclePeriod = recurringCyclePeriod;
                //RecurringTotalCycles = recurringTotalCycles;
            }
            return details;
        }

        public virtual bool ValidateMinOrderSubtotalAmount(IList<ShoppingCartItem> cart)
        {
            if (cart == null)
                throw new ArgumentNullException(nameof(cart));

            //min order amount sub-total validation
            if (cart.Any() && _orderSettings.MinOrderSubtotalAmount > decimal.Zero)
            {
                //subtotal
                _orderTotalCalculationService.GetShoppingCartSubTotal(cart, _orderSettings.MinOrderSubtotalAmountIncludingTax, out decimal _, out List<DiscountForCaching> _, out decimal subTotalWithoutDiscountBase, out decimal _);

                if (subTotalWithoutDiscountBase < _orderSettings.MinOrderSubtotalAmount)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Validate minimum order total amount
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>true - OK; false - minimum order total amount is not reached</returns>
        public virtual bool ValidateMinOrderTotalAmount(IList<ShoppingCartItem> cart)
        {
            if (cart == null)
                throw new ArgumentNullException(nameof(cart));

            if (cart.Any() && _orderSettings.MinOrderTotalAmount > decimal.Zero)
            {
                var shoppingCartTotalBase = _orderTotalCalculationService.GetShoppingCartTotal(cart);
                if (shoppingCartTotalBase.HasValue && shoppingCartTotalBase.Value < _orderSettings.MinOrderTotalAmount)
                    return false;
            }

            return true;
        }


        //nested
        protected class PlaceOrderContainer
        {
            /// <summary>
            /// Ctor
            /// </summary>
            public PlaceOrderContainer()
            {
                this.Cart = new List<ShoppingCartItem>();
                this.AppliedDiscounts = new List<DiscountForCaching>();
                this.AppliedGiftCards = new List<AppliedGiftCard>();
            }

            /// <summary>
            /// Customer
            /// </summary>
            public Customer Customer { get; set; }
            /// <summary>
            /// Customer language
            /// </summary>
            public Language CustomerLanguage { get; set; }
            /// <summary>
            /// Affiliate identifier
            /// </summary>
            public int AffiliateId { get; set; }
            /// <summary>
            /// TAx display type
            /// </summary>
            public TaxDisplayType CustomerTaxDisplayType { get; set; }
            /// <summary>
            /// Selected currency
            /// </summary>
            public string CustomerCurrencyCode { get; set; }
            /// <summary>
            /// Customer currency rate
            /// </summary>
            public decimal CustomerCurrencyRate { get; set; }

            /// <summary>
            /// Billing address
            /// </summary>
            public Address BillingAddress { get; set; }
            /// <summary>
            /// Shipping address
            /// </summary>
            public Address ShippingAddress { get; set; }
            /// <summary>
            /// Shipping status
            /// </summary>
            public ShippingStatus ShippingStatus { get; set; }
            /// <summary>
            /// Selected shipping method
            /// </summary>
            public string ShippingMethodName { get; set; }
            /// <summary>
            /// Shipping rate computation method system name
            /// </summary>
            public string ShippingRateComputationMethodSystemName { get; set; }
            /// <summary>
            /// Is pickup in store selected?
            /// </summary>
            public bool PickUpInStore { get; set; }
            /// <summary>
            /// Selected pickup address
            /// </summary>
            public Address PickupAddress { get; set; }

            /// <summary>
            /// Is recurring shopping cart
            /// </summary>
            public bool IsRecurringShoppingCart { get; set; }
            /// <summary>
            /// Initial order (used with recurring payments)
            /// </summary>
            public Order InitialOrder { get; set; }

            /// <summary>
            /// Checkout attributes
            /// </summary>
            public string CheckoutAttributeDescription { get; set; }
            /// <summary>
            /// Shopping cart
            /// </summary>
            public string CheckoutAttributesXml { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public IList<ShoppingCartItem> Cart { get; set; }
            /// <summary>
            /// Applied discounts
            /// </summary>
            public List<DiscountForCaching> AppliedDiscounts { get; set; }
            /// <summary>
            /// Applied gift cards
            /// </summary>
            public List<AppliedGiftCard> AppliedGiftCards { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public decimal OrderSubTotalInclTax { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public decimal OrderSubTotalExclTax { get; set; }
            /// <summary>
            /// Subtotal discount (incl tax)
            /// </summary>
            public decimal OrderSubTotalDiscountInclTax { get; set; }
            /// <summary>
            /// Subtotal discount (excl tax)
            /// </summary>
            public decimal OrderSubTotalDiscountExclTax { get; set; }
            /// <summary>
            /// Shipping (incl tax)
            /// </summary>
            public decimal OrderShippingTotalInclTax { get; set; }
            /// <summary>
            /// Shipping (excl tax)
            /// </summary>
            public decimal OrderShippingTotalExclTax { get; set; }
            /// <summary>
            /// Payment additional fee (incl tax)
            /// </summary>
            public decimal PaymentAdditionalFeeInclTax { get; set; }
            /// <summary>
            /// Payment additional fee (excl tax)
            /// </summary>
            public decimal PaymentAdditionalFeeExclTax { get; set; }
            /// <summary>
            /// Tax
            /// </summary>
            public decimal OrderTaxTotal { get; set; }
            /// <summary>
            /// VAT number
            /// </summary>
            public string VatNumber { get; set; }
            /// <summary>
            /// Tax rates
            /// </summary>
            public string TaxRates { get; set; }
            /// <summary>
            /// Order total discount amount
            /// </summary>
            public decimal OrderDiscountAmount { get; set; }
            /// <summary>
            /// Redeemed reward points
            /// </summary>
            public int RedeemedRewardPoints { get; set; }
            /// <summary>
            /// Redeemed reward points amount
            /// </summary>
            public decimal RedeemedRewardPointsAmount { get; set; }
            /// <summary>
            /// Order total
            /// </summary>
            public decimal OrderTotal { get; set; }
        }
    }
}