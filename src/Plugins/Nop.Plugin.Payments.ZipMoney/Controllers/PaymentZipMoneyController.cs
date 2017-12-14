using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Affiliates;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Core.Http.Extensions;
using Nop.Plugin.Payments.ZipMoney.Models;
using Nop.Services.Affiliates;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Web.Factories;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Models.Checkout;
using ZipMoneySDK;
using ZipMoneySDK.Models;

namespace Nop.Plugin.Payments.ZipMoney.Controllers
{
    public class PaymentZipMoneyController : BasePaymentController
    {
        private readonly IAffiliateService _affiliateService;
        private readonly ICheckoutAttributeFormatter _checkoutAttributeFormatter;
        private readonly ICheckoutModelFactory _checkoutModelFactory;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly ICustomerService _customerService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderService _orderService;
        private readonly OrderSettings _orderSettings;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IPaymentService _paymentService;
        private readonly PaymentSettings _paymentSettings;
        private readonly IPermissionService _permissionService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductService _productService;
        private readonly ISettingService _settingService;
        private readonly ShippingSettings _shippingSettings;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreService _storeService;
        private readonly ITaxService _taxService;
        private readonly TaxSettings _taxSettings;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;

        public PaymentZipMoneyController(ISettingService settingService,
            IWorkContext workContext,
            IStoreService storeService,
            IPaymentService paymentService, IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            IWebHelper webHelper,
            IStoreContext storeContext,
            PaymentSettings paymentSettings,
            IShoppingCartService shoppingCartService,
            IPermissionService permissionService,
            ICustomerService customerService,
            ICurrencyService currencyService,
            CurrencySettings currencySettings,
            ILanguageService languageService,
            IAffiliateService affiliateService,
            OrderSettings orderSettings,
            TaxSettings taxSettings,
            IPriceFormatter priceFormatter,
            IOrderTotalCalculationService orderTotalCalculationService,
            IProductAttributeFormatter productAttributeFormatter,
            IProductService productService,
            ICheckoutAttributeFormatter checkoutAttributeFormatter,
            IProductAttributeParser productAttributeParser,
            ShippingSettings shippingSettings,
            ICountryService countryService,
            IStateProvinceService stateProvinceService,
            ITaxService taxService,
            IGenericAttributeService genericAttributeService,
            ICheckoutModelFactory checkoutModelFactory,
            ILogger logger,
            ILocalizationService localizationService)
        {
            _workContext = workContext;
            _storeService = storeService;
            _settingService = settingService;
            _paymentService = paymentService;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _webHelper = webHelper;
            _paymentSettings = paymentSettings;
            _localizationService = localizationService;
            _shoppingCartService = shoppingCartService;
            _permissionService = permissionService;
            _customerService = customerService;
            _storeContext = storeContext;
            _currencySettings = currencySettings;
            _affiliateService = affiliateService;
            _languageService = languageService;
            _currencyService = currencyService;
            _orderSettings = orderSettings;
            _taxSettings = taxSettings;
            _priceFormatter = priceFormatter;
            _orderTotalCalculationService = orderTotalCalculationService;
            _productAttributeFormatter = productAttributeFormatter;
            _productService = productService;
            _checkoutAttributeFormatter = checkoutAttributeFormatter;
            _productAttributeParser = productAttributeParser;
            _shippingSettings = shippingSettings;
            _countryService = countryService;
            _stateProvinceService = stateProvinceService;
            _genericAttributeService = genericAttributeService;
            _checkoutModelFactory = checkoutModelFactory;
            _logger = logger;
            _taxService = taxService;
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            //whether user has the authority
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            int storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            ZipMoneyPaymentSettings zipMoneyPaymentSettings =
                _settingService.LoadSetting<ZipMoneyPaymentSettings>(storeScope);

            ConfigurationModel model = new ConfigurationModel
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
        [HttpPost]
        public IActionResult Configure(ConfigurationModel model)
        {
            //whether user has the authority
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            int storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            ZipMoneyPaymentSettings zipMoneyPaymentSettings =
                _settingService.LoadSetting<ZipMoneyPaymentSettings>(storeScope);

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
            Order order = _orderService.SearchOrders(_storeContext.CurrentStore.Id,
                customerId: _workContext.CurrentCustomer.Id, pageSize: 1).FirstOrDefault();
            if (order != null)
                return RedirectToRoute("OrderDetails", new {orderId = order.Id});

            return RedirectToRoute("HomePage");
        }

        public async Task<string> ZipCheckout()
        {
            int storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            ZipMoneyPaymentSettings zipMoneyPaymentSettings =
                _settingService.LoadSetting<ZipMoneyPaymentSettings>(storeScope);
            ZipCheckoutRequest zipCheckout = new ZipCheckoutRequest();
            string apikey = zipMoneyPaymentSettings.UseSandbox
                ? zipMoneyPaymentSettings.SandboxAPIKey
                : zipMoneyPaymentSettings.ProductionAPIKey;
            PlaceOrderContainer details = GetOrderDetails();
            var orders = _orderService.SearchOrders(customerId: _workContext.CurrentCustomer.Id);
            var orderscount = orders.Count;
            var salestotalamount = orders.Sum(x => x.OrderTotal);
            decimal salesavgamount = 0;
            try
            {
                salesavgamount = orders.Average(x => x.OrderTotal);
            }
            catch (InvalidOperationException)
            {
            }
            decimal salesmaxamount = 0;
            try
            {
                salesmaxamount = orders.Max(x => x.OrderTotal);
            }
            catch (InvalidOperationException)
            {
                
            }
            var refundstotalamount = orders.Sum(x => x.RefundedAmount);
            zipCheckout.shopper = new ZipShopper
            {
                billing_address = new ZipAddress
                {
                    first_name = details.BillingAddress.FirstName,
                    last_name = details.BillingAddress.LastName,
                    line1 = details.BillingAddress.Address1,
                    line2 = details.BillingAddress.Address2,
                    city = details.BillingAddress.City,
                    country = details.BillingAddress.Country.TwoLetterIsoCode,
                    postal_code = details.BillingAddress.ZipPostalCode,
                    state = details.BillingAddress.StateProvince.Name
                },
                email = details.BillingAddress.Email,
                phone = details.BillingAddress.PhoneNumber.Replace(" ", ""),
                first_name = details.BillingAddress.FirstName,
                last_name = details.BillingAddress.LastName,
                statistics = new ZipStatistics
                {
                    account_created = details.Customer.CreatedOnUtc,
                    currency = "AUD",
                    sales_total_count = orderscount,
                    sales_total_amount = salestotalamount,
                    sales_avg_amount = salesavgamount,
                    sales_max_amount = salesmaxamount,
                    refunds_total_amount = refundstotalamount
                }
            };
            zipCheckout.order = new ZipCheckoutOrder
            {
                amount = details.OrderTotal,
                currency = "AUD",
                shipping = new ZipShipping
                {
                    pickup = details.PickUpInStore,
                },
                items = new List<ZipOrderItem>()
            };
            if (details.ShippingAddress != null)
            {
                zipCheckout.order.shipping.address = new ZipAddress
                {
                    line1 = details.ShippingAddress.Address1,
                    line2 = details.ShippingAddress.Address2,
                    first_name = details.ShippingAddress.FirstName,
                    last_name = details.ShippingAddress.LastName,
                    city = details.ShippingAddress.City,
                    country = details.ShippingAddress.Country.TwoLetterIsoCode,
                    state = details.ShippingAddress.StateProvince.Name,
                    postal_code = details.ShippingAddress.ZipPostalCode
                };
            }
            else
            {
                zipCheckout.order.shipping.pickup = true;
            }
            if (details.Customer.LastLoginDateUtc != null)
                zipCheckout.shopper.statistics.last_login = details.Customer.LastLoginDateUtc.Value;

            foreach (ShoppingCartItem item in details.Cart)
            {
                ZipOrderItem zipOrderItem = new ZipOrderItem
                {
                    amount = item.Product.Price,
                    name = item.Product.Name,
                    quantity = item.Quantity,
                    type = OrderType.sku,
                    product_code = item.Product.Sku,
                };

                string url = _storeContext.CurrentStore.SslEnabled ? _storeContext.CurrentStore.SecureUrl : _storeContext.CurrentStore.Url + "/content/images/";
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
            ZipOrderItem taxItem = new ZipOrderItem();
            ZipOrderItem shipItem = new ZipOrderItem
            {
                amount = details.OrderShippingTotalInclTax,
                name = "Shipping",
                quantity = 1,
                type = OrderType.shipping,
                reference = "shipping"
            };
            zipCheckout.order.items.Add(shipItem);
            ZipOrderItem discountItem = new ZipOrderItem
            {
                amount = details.OrderDiscountAmount,
                name = "discount",
                quantity = 1,
                type = OrderType.discount,
                reference = "discount"
            };
            zipCheckout.order.items.Add(discountItem);

            
            zipCheckout.config = new ZipConfig
            {
                redirect_uri = _storeContext.CurrentStore.SslEnabled ? _storeContext.CurrentStore.SecureUrl : _storeContext.CurrentStore.Url + "/PaymentZipMoney/ZipRedirect"
            };
            ZipMoneyProcessor zm = new ZipMoneyProcessor(apikey, true);
            _logger.InsertLog(LogLevel.Debug,"Zip checkoutrequest",JsonConvert.SerializeObject(zipCheckout));
            ZipCheckoutResponse zcr = await zm.CreateCheckout(zipCheckout);
            _logger.InsertLog(LogLevel.Debug,"zip checkoutresponse",zm.GetLastResponse());
            if (zm.GetLastError() != null)
            {
                _logger.InsertLog(LogLevel.Error, "zip error", zm.GetLastResponse());
            }
            _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, "ZipCheckoutId", zcr.id,
                _storeContext.CurrentStore.Id);
            return JsonConvert.SerializeObject(zcr);
        }

        public IActionResult ZipRedirect(string result, string checkoutid)
        {
            _logger.InsertLog(LogLevel.Debug, "ZipRedirect called. " + result + ". " + checkoutid, "",
                _workContext.CurrentCustomer);
            if (result == null) return null;
            if (checkoutid == null) return null;
            if (result.ToLowerInvariant().Equals("approved"))
            {
                string savedcheckoutId = _workContext.CurrentCustomer
                    .GetAttribute<string>("ZipCheckoutId", _genericAttributeService, _storeContext.CurrentStore.Id);
                if (savedcheckoutId != null && savedcheckoutId.Equals(checkoutid))
                {
//same session
                    PlaceOrderContainer details = GetOrderDetails();
                    decimal amount = details.OrderTotal;
                    int storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
                    ZipMoneyPaymentSettings zipMoneyPaymentSettings =
                        _settingService.LoadSetting<ZipMoneyPaymentSettings>(storeScope);
                    string apikey = zipMoneyPaymentSettings.UseSandbox
                        ? zipMoneyPaymentSettings.SandboxAPIKey
                        : zipMoneyPaymentSettings.ProductionAPIKey;
                    ZipChargeRequest zipCharge = new ZipChargeRequest
                    {
                        authority = new ZipAuthority
                        {
                            type = AuthorityType.checkout_id,
                            value = checkoutid
                        },
                        capture = false,
                        amount = amount,
                        currency = "AUD"
                    };
                    _logger.InsertLog(LogLevel.Debug, "ZipCharge JSON", JsonConvert.SerializeObject(zipCharge),
                        _workContext.CurrentCustomer);
                    ZipMoneyProcessor zm = new ZipMoneyProcessor(apikey, true);
                    ZipChargeResponse response;
                    try
                    {
                        response = zm.CreateCharge(zipCharge).Result;
                        _logger.InsertLog(LogLevel.Debug, "ZipCharge Result JSON", zm.GetLastResponse(),
                            _workContext.CurrentCustomer);
                    }
                    catch (Newtonsoft.Json.JsonSerializationException e)
                    {
                        _logger.InsertLog(LogLevel.Error, "Newtonsoft.Json.JsonSerializationException", e.Message);
                        _logger.InsertLog(LogLevel.Debug, "ZipCharge Result JSON", zm.GetLastResponse(),
                            _workContext.CurrentCustomer);
                        HttpContext.Session.SetString("ZipFriendlyError","There was an error euahtorising your payment. We received an invalid response. Please try again");
                        HttpContext.Session.SetInt32("ZipShowError", 1);
                        return RedirectToRoute("CheckoutPaymentMethod");
                    }
                    
                    if (zm.GetLastError() != null)
                    {
                        _logger.InsertLog(LogLevel.Error, "ZipMoney Error",
                            zm.GetLastResponse(),
                            _workContext.CurrentCustomer);
                        switch (zm.GetLastError().error.code)
                        {
                            case "account_insufficient_funds":
                                HttpContext.Session.SetString("ZipFriendlyError",
                                    "There was an error authorising your payment. Please ensure you have enough funds or choose a different payment method");
                                break;
                            case "account_inoperative":
                                HttpContext.Session.SetString("ZipFriendlyError",
                                    "There was an error authorising your payment. Your account appears to be in arrears or is closed");
                                break;
                            case "account_locked":
                                HttpContext.Session.SetString("ZipFriendlyError",
                                    "There was an error authorising your payment. Your account appears to be in arrears or is closed");
                                break;
                            case "amount_invalid":
                                HttpContext.Session.SetString("ZipFriendlyError",
                                    "There was an error authorising your payment. Your account appears to be in arrears or is closed");
                                break;
                            case "fraud_check":
                                HttpContext.Session.SetString("ZipFriendlyError",
                                    "There was an error authorising your payment. Your account appears to be in arrears or is closed");
                                break;

                        }
                        HttpContext.Session.SetInt32("ZipShowError", 1);
                        return RedirectToRoute("CheckoutPaymentMethod");
                    }
                    var content = new Dictionary<string, StringValues>
                    {
                        {"nextstep", "Next"},
                        {"ZipCheckoutId", checkoutid},
                        {"ZipChargeId", response.id},
                        {"ZipChargeState", response.state.ToString()},
                        {"ZipCheckoutResult", result.ToLowerInvariant()}
                    };
                    FormCollection collection = new FormCollection(content);
                    return EnterPaymentInfo(collection);
                }
                _logger.InsertLog(LogLevel.Debug, "Searching for checkoutid");
                //different session. most likely referral that is now approved
                Order order =
                    _orderService.GetOrderByAuthorizationTransactionIdAndPaymentMethod(checkoutid, "Payments.ZipMoney");
                if (order != null)
                {
                    _logger.InsertLog(LogLevel.Debug, "Searching for checkoutid: found");
                    _orderProcessingService.MarkAsAuthorized(order);
                    var errors = _orderProcessingService.Capture(order);
                    if (order.PaymentStatus == PaymentStatus.Paid)
                        return RedirectToRoute("CheckoutCompleted", new {orderId = order.Id});
                    _logger.InsertLog(LogLevel.Error, "Could not capture transaction. Checkout_id=" + checkoutid,
                        errors.ToString());
                }
                else
                {
                    _logger.InsertLog(LogLevel.Error, "Order not found");
                }
            }
            else if (result.ToLowerInvariant().Equals("cancelled"))
            {
                HttpContext.Session.SetString("ZipFriendlyError",
                    "You have chosen not to proceed with ZipMoney. Please choose a different payment method to continue");
                HttpContext.Session.SetInt32("ZipShowError", 1);
                return RedirectToRoute("CheckoutPaymentMethod");
            }
            else if (result.ToLowerInvariant().Equals("referred"))
            {
                var content = new Dictionary<string, StringValues>
                {
                    {"nextstep", "Next"},
                    {"ZipCheckoutId", checkoutid},
                    {"ZipChargeId", ""},
                    {"ZipChargeState", ""},
                    {"ZipCheckoutResult", result.ToLowerInvariant()}
                };
                HttpContext.Session.SetString("ZipReferred1",
                    "Your payment is currently pending approval. You may choose to place the order now hang on until you receive an email with the decision");
                HttpContext.Session.SetString("ZipReferred2",
                    "If approved you will be able to complete the payment following the link in the email");
                HttpContext.Session.SetString("ZipReferred3",
                    "If declined or you do not complete your payment within 2 days we will cancel your purchase");
                HttpContext.Session.SetInt32("ZipShowReferred", 1);
                FormCollection collection = new FormCollection(content);
                return EnterPaymentInfo(collection);
            }
            else if (result.ToLowerInvariant().Equals("declined"))
            {
                HttpContext.Session.SetString("ZipFriendlyError",
                    "Unfortunately your ZipMoney application was declined. Please choose a different payment method to continue");
                HttpContext.Session.SetInt32("ZipShowError", 1);
                return RedirectToRoute("CheckoutPaymentMethod");
            }
            //if all else fails go back to the shopping cart
            return RedirectToRoute("ShoppingCart");
        }

        public virtual IActionResult EnterPaymentInfo(IFormCollection form)
        {
            //validation
            List<ShoppingCartItem> cart = _workContext.CurrentCustomer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                .LimitPerStore(_storeContext.CurrentStore.Id)
                .ToList();
            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            //Check whether payment workflow is required
            bool isPaymentWorkflowRequired = _orderProcessingService.IsPaymentWorkflowRequired(cart);
            if (!isPaymentWorkflowRequired)
                return RedirectToRoute("CheckoutConfirm");

            //load payment method
            string paymentMethodSystemName = _workContext.CurrentCustomer
                .GetAttribute<string>(SystemCustomerAttributeNames.SelectedPaymentMethod, _genericAttributeService,
                    _storeContext.CurrentStore.Id);
            IPaymentMethod paymentMethod = _paymentService.LoadPaymentMethodBySystemName(paymentMethodSystemName);
            if (paymentMethod == null)
                return RedirectToRoute("CheckoutPaymentMethod");

            IList<string> warnings = paymentMethod.ValidatePaymentForm(form);
            foreach (string warning in warnings)
                ModelState.AddModelError("", warning);
            if (ModelState.IsValid)
            {
                //get payment info
                ProcessPaymentRequest paymentInfo = paymentMethod.GetPaymentInfo(form);

                //session save
                HttpContext.Session.Set("OrderPaymentInfo", paymentInfo);
                return RedirectToRoute("CheckoutConfirm");
            }

            //If we got this far, something failed, redisplay form
            //model
            CheckoutPaymentInfoModel model = _checkoutModelFactory.PreparePaymentInfoModel(paymentMethod);
            return View(model);
        }

        private PlaceOrderContainer GetOrderDetails()
        {
            PlaceOrderContainer details = new PlaceOrderContainer();
            int StoreId = _storeContext.CurrentStore.Id;
            //customer
            details.Customer = _customerService.GetCustomerById(_workContext.CurrentCustomer.Id);

            //affiliate
            Affiliate affiliate = _affiliateService.GetAffiliateById(details.Customer.AffiliateId);
            if (affiliate != null && affiliate.Active && !affiliate.Deleted)
                details.AffiliateId = affiliate.Id;

            //customer currency
            Currency currencyTmp = _currencyService.GetCurrencyById(
                details.Customer.GetAttribute<int>(SystemCustomerAttributeNames.CurrencyId, StoreId));
            Currency customerCurrency = currencyTmp != null && currencyTmp.Published
                ? currencyTmp
                : _workContext.WorkingCurrency;
            Currency primaryStoreCurrency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
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

            details.BillingAddress = (Address) details.Customer.BillingAddress.Clone();
            if (details.BillingAddress.Country != null && !details.BillingAddress.Country.AllowsBilling)
                throw new NopException($"Country '{details.BillingAddress.Country.Name}' is not allowed for billing");

            //checkout attributes
            details.CheckoutAttributesXml =
                details.Customer.GetAttribute<string>(SystemCustomerAttributeNames.CheckoutAttributes, StoreId);
            details.CheckoutAttributeDescription =
                _checkoutAttributeFormatter.FormatAttributes(details.CheckoutAttributesXml, details.Customer);

            //load shopping cart
            details.Cart = details.Customer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                .LimitPerStore(StoreId).ToList();

            if (!details.Cart.Any())
                throw new NopException("Cart is empty");

            //validate the entire shopping cart
            IList<string> warnings =
                _shoppingCartService.GetShoppingCartWarnings(details.Cart, details.CheckoutAttributesXml, true);
            if (warnings.Any())
                throw new NopException(warnings.Aggregate(string.Empty, (current, next) => $"{current}{next};"));

            //validate individual cart items
            foreach (ShoppingCartItem sci in details.Cart)
            {
                IList<string> sciWarnings = _shoppingCartService.GetShoppingCartItemWarnings(details.Customer,
                    sci.ShoppingCartType, sci.Product, StoreId, sci.AttributesXml,
                    sci.CustomerEnteredPrice, sci.RentalStartDateUtc, sci.RentalEndDateUtc, sci.Quantity, false);
                if (sciWarnings.Any())
                    throw new NopException(sciWarnings.Aggregate(string.Empty, (current, next) => $"{current}{next};"));
            }

            //min totals validation
            if (!ValidateMinOrderSubtotalAmount(details.Cart))
            {
                decimal minOrderSubtotalAmount =
                    _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderSubtotalAmount,
                        _workContext.WorkingCurrency);
                throw new NopException(string.Format(
                    _localizationService.GetResource("Checkout.MinOrderSubtotalAmount"),
                    _priceFormatter.FormatPrice(minOrderSubtotalAmount, true, false)));
            }

            if (!ValidateMinOrderTotalAmount(details.Cart))
            {
                decimal minOrderTotalAmount =
                    _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderTotalAmount,
                        _workContext.WorkingCurrency);
                throw new NopException(string.Format(_localizationService.GetResource("Checkout.MinOrderTotalAmount"),
                    _priceFormatter.FormatPrice(minOrderTotalAmount, true, false)));
            }

            //tax display type
            if (_taxSettings.AllowCustomersToSelectTaxDisplayType)
                details.CustomerTaxDisplayType =
                    (TaxDisplayType) details.Customer.GetAttribute<int>(SystemCustomerAttributeNames.TaxDisplayTypeId,
                        StoreId);
            else
                details.CustomerTaxDisplayType = _taxSettings.TaxDisplayType;

            //sub total (incl tax)
            _orderTotalCalculationService.GetShoppingCartSubTotal(details.Cart, true,
                out decimal orderSubTotalDiscountAmount,
                out List<DiscountForCaching> orderSubTotalAppliedDiscounts, out decimal subTotalWithoutDiscountBase,
                out decimal _);
            details.OrderSubTotalInclTax = subTotalWithoutDiscountBase;
            details.OrderSubTotalDiscountInclTax = orderSubTotalDiscountAmount;

            //discount history
            foreach (DiscountForCaching disc in orderSubTotalAppliedDiscounts)
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
                PickupPoint pickupPoint =
                    details.Customer.GetAttribute<PickupPoint>(SystemCustomerAttributeNames.SelectedPickupPoint,
                        StoreId);
                if (_shippingSettings.AllowPickUpInStore && pickupPoint != null)
                {
                    Country country = _countryService.GetCountryByTwoLetterIsoCode(pickupPoint.CountryCode);
                    StateProvince state = _stateProvinceService.GetStateProvinceByAbbreviation(
                        pickupPoint.StateAbbreviation,
                        country?.Id);

                    details.PickUpInStore = true;
                    details.PickupAddress = new Address
                    {
                        Address1 = pickupPoint.Address,
                        City = pickupPoint.City,
                        Country = country,
                        StateProvince = state,
                        ZipPostalCode = pickupPoint.ZipPostalCode,
                        CreatedOnUtc = DateTime.UtcNow
                    };
                }
                else
                {
                    if (details.Customer.ShippingAddress == null)
                        throw new NopException("Shipping address is not provided");

                    if (!CommonHelper.IsValidEmail(details.Customer.ShippingAddress.Email))
                        throw new NopException("Email is not valid");

                    //clone shipping address
                    details.ShippingAddress = (Address) details.Customer.ShippingAddress.Clone();
                    if (details.ShippingAddress.Country != null && !details.ShippingAddress.Country.AllowsShipping)
                        throw new NopException(
                            $"Country '{details.ShippingAddress.Country.Name}' is not allowed for shipping");
                }

                ShippingOption shippingOption =
                    details.Customer.GetAttribute<ShippingOption>(SystemCustomerAttributeNames.SelectedShippingOption,
                        StoreId);
                if (shippingOption != null)
                {
                    details.ShippingMethodName = shippingOption.Name;
                    details.ShippingRateComputationMethodSystemName =
                        shippingOption.ShippingRateComputationMethodSystemName;
                }

                details.ShippingStatus = ShippingStatus.NotYetShipped;
            }
            else
            {
                details.ShippingStatus = ShippingStatus.ShippingNotRequired;
            }

            //shipping total
            decimal? orderShippingTotalInclTax = _orderTotalCalculationService.GetShoppingCartShippingTotal(
                details.Cart, true,
                out decimal _, out List<DiscountForCaching> shippingTotalDiscounts);
            decimal? orderShippingTotalExclTax =
                _orderTotalCalculationService.GetShoppingCartShippingTotal(details.Cart, false);
            if (!orderShippingTotalInclTax.HasValue || !orderShippingTotalExclTax.HasValue)
                throw new NopException("Shipping total couldn't be calculated");

            details.OrderShippingTotalInclTax = orderShippingTotalInclTax.Value;
            details.OrderShippingTotalExclTax = orderShippingTotalExclTax.Value;

            foreach (DiscountForCaching disc in shippingTotalDiscounts)
                if (!details.AppliedDiscounts.ContainsDiscount(disc))
                    details.AppliedDiscounts.Add(disc);

            //payment total
            decimal paymentAdditionalFee =
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
            VatNumberStatus customerVatStatus =
                (VatNumberStatus) details.Customer.GetAttribute<int>(SystemCustomerAttributeNames.VatNumberStatusId);
            if (_taxSettings.EuVatEnabled && customerVatStatus == VatNumberStatus.Valid)
                details.VatNumber = details.Customer.GetAttribute<string>(SystemCustomerAttributeNames.VatNumber);

            //tax rates
            details.TaxRates = taxRatesDictionary.Aggregate(string.Empty, (current, next) =>
                $"{current}{next.Key.ToString(CultureInfo.InvariantCulture)}:{next.Value.ToString(CultureInfo.InvariantCulture)};   ");

            //order total (and applied discounts, gift cards, reward points)
            decimal? orderTotal = _orderTotalCalculationService.GetShoppingCartTotal(details.Cart,
                out decimal orderDiscountAmount,
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
            foreach (DiscountForCaching disc in orderAppliedDiscounts)
                if (!details.AppliedDiscounts.ContainsDiscount(disc))
                    details.AppliedDiscounts.Add(disc);


            //recurring or standard shopping cart?
            details.IsRecurringShoppingCart = details.Cart.IsRecurring();
            if (details.IsRecurringShoppingCart)
            {
                string recurringCyclesError = details.Cart.GetRecurringCycleInfo(_localizationService,
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
                _orderTotalCalculationService.GetShoppingCartSubTotal(cart,
                    _orderSettings.MinOrderSubtotalAmountIncludingTax, out decimal _, out List<DiscountForCaching> _,
                    out decimal subTotalWithoutDiscountBase, out decimal _);

                if (subTotalWithoutDiscountBase < _orderSettings.MinOrderSubtotalAmount)
                    return false;
            }

            return true;
        }

        /// <summary>
        ///     Validate minimum order total amount
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>true - OK; false - minimum order total amount is not reached</returns>
        public virtual bool ValidateMinOrderTotalAmount(IList<ShoppingCartItem> cart)
        {
            if (cart == null)
                throw new ArgumentNullException(nameof(cart));

            if (cart.Any() && _orderSettings.MinOrderTotalAmount > decimal.Zero)
            {
                decimal? shoppingCartTotalBase = _orderTotalCalculationService.GetShoppingCartTotal(cart);
                if (shoppingCartTotalBase.HasValue && shoppingCartTotalBase.Value < _orderSettings.MinOrderTotalAmount)
                    return false;
            }

            return true;
        }


        //nested
        protected class PlaceOrderContainer
        {
            /// <summary>
            ///     Ctor
            /// </summary>
            public PlaceOrderContainer()
            {
                Cart = new List<ShoppingCartItem>();
                AppliedDiscounts = new List<DiscountForCaching>();
                AppliedGiftCards = new List<AppliedGiftCard>();
            }

            /// <summary>
            ///     Customer
            /// </summary>
            public Customer Customer { get; set; }

            /// <summary>
            ///     Customer language
            /// </summary>
            public Language CustomerLanguage { get; set; }

            /// <summary>
            ///     Affiliate identifier
            /// </summary>
            public int AffiliateId { get; set; }

            /// <summary>
            ///     TAx display type
            /// </summary>
            public TaxDisplayType CustomerTaxDisplayType { get; set; }

            /// <summary>
            ///     Selected currency
            /// </summary>
            public string CustomerCurrencyCode { get; set; }

            /// <summary>
            ///     Customer currency rate
            /// </summary>
            public decimal CustomerCurrencyRate { get; set; }

            /// <summary>
            ///     Billing address
            /// </summary>
            public Address BillingAddress { get; set; }

            /// <summary>
            ///     Shipping address
            /// </summary>
            public Address ShippingAddress { get; set; }

            /// <summary>
            ///     Shipping status
            /// </summary>
            public ShippingStatus ShippingStatus { get; set; }

            /// <summary>
            ///     Selected shipping method
            /// </summary>
            public string ShippingMethodName { get; set; }

            /// <summary>
            ///     Shipping rate computation method system name
            /// </summary>
            public string ShippingRateComputationMethodSystemName { get; set; }

            /// <summary>
            ///     Is pickup in store selected?
            /// </summary>
            public bool PickUpInStore { get; set; }

            /// <summary>
            ///     Selected pickup address
            /// </summary>
            public Address PickupAddress { get; set; }

            /// <summary>
            ///     Is recurring shopping cart
            /// </summary>
            public bool IsRecurringShoppingCart { get; set; }

            /// <summary>
            ///     Initial order (used with recurring payments)
            /// </summary>
            public Order InitialOrder { get; set; }

            /// <summary>
            ///     Checkout attributes
            /// </summary>
            public string CheckoutAttributeDescription { get; set; }

            /// <summary>
            ///     Shopping cart
            /// </summary>
            public string CheckoutAttributesXml { get; set; }

            /// <summary>
            /// </summary>
            public IList<ShoppingCartItem> Cart { get; set; }

            /// <summary>
            ///     Applied discounts
            /// </summary>
            public List<DiscountForCaching> AppliedDiscounts { get; set; }

            /// <summary>
            ///     Applied gift cards
            /// </summary>
            public List<AppliedGiftCard> AppliedGiftCards { get; set; }

            /// <summary>
            /// </summary>
            public decimal OrderSubTotalInclTax { get; set; }

            /// <summary>
            /// </summary>
            public decimal OrderSubTotalExclTax { get; set; }

            /// <summary>
            ///     Subtotal discount (incl tax)
            /// </summary>
            public decimal OrderSubTotalDiscountInclTax { get; set; }

            /// <summary>
            ///     Subtotal discount (excl tax)
            /// </summary>
            public decimal OrderSubTotalDiscountExclTax { get; set; }

            /// <summary>
            ///     Shipping (incl tax)
            /// </summary>
            public decimal OrderShippingTotalInclTax { get; set; }

            /// <summary>
            ///     Shipping (excl tax)
            /// </summary>
            public decimal OrderShippingTotalExclTax { get; set; }

            /// <summary>
            ///     Payment additional fee (incl tax)
            /// </summary>
            public decimal PaymentAdditionalFeeInclTax { get; set; }

            /// <summary>
            ///     Payment additional fee (excl tax)
            /// </summary>
            public decimal PaymentAdditionalFeeExclTax { get; set; }

            /// <summary>
            ///     Tax
            /// </summary>
            public decimal OrderTaxTotal { get; set; }

            /// <summary>
            ///     VAT number
            /// </summary>
            public string VatNumber { get; set; }

            /// <summary>
            ///     Tax rates
            /// </summary>
            public string TaxRates { get; set; }

            /// <summary>
            ///     Order total discount amount
            /// </summary>
            public decimal OrderDiscountAmount { get; set; }

            /// <summary>
            ///     Redeemed reward points
            /// </summary>
            public int RedeemedRewardPoints { get; set; }

            /// <summary>
            ///     Redeemed reward points amount
            /// </summary>
            public decimal RedeemedRewardPointsAmount { get; set; }

            /// <summary>
            ///     Order total
            /// </summary>
            public decimal OrderTotal { get; set; }
        }
    }
}