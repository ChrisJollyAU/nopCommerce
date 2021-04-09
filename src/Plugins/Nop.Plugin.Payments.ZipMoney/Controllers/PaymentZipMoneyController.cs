using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
using Nop.Core.Domain.Discounts;
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
using Nop.Services.Media;
using Nop.Services.Messages;
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
        private readonly IPermissionService _permissionService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductService _productService;
        private readonly ISettingService _settingService;
        private readonly ShippingSettings _shippingSettings;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreContext _storeContext;
        private readonly ITaxService _taxService;
        private readonly TaxSettings _taxSettings;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly INotificationService _notificationService;
        private readonly IDiscountService _discountService;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IAddressService _addressService;
        private readonly IPictureService _pictureService;

        public PaymentZipMoneyController(ISettingService settingService,
            IWorkContext workContext,
            IPaymentService paymentService, IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            IWebHelper webHelper,
            IStoreContext storeContext,
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
            IProductService productService,
            ICheckoutAttributeFormatter checkoutAttributeFormatter,
            ShippingSettings shippingSettings,
            ICountryService countryService,
            IStateProvinceService stateProvinceService,
            ITaxService taxService,
            IGenericAttributeService genericAttributeService,
            ICheckoutModelFactory checkoutModelFactory,
            ILogger logger,
            INotificationService notificationService,
            IPaymentPluginManager paymentPluginManager,
            IDiscountService discountService,
            IAddressService addressService,
            IPictureService pictureService,
            ILocalizationService localizationService)
        {
            _workContext = workContext;
            _settingService = settingService;
            _paymentService = paymentService;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _webHelper = webHelper;
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
            _productService = productService;
            _checkoutAttributeFormatter = checkoutAttributeFormatter;
            _shippingSettings = shippingSettings;
            _countryService = countryService;
            _stateProvinceService = stateProvinceService;
            _genericAttributeService = genericAttributeService;
            _checkoutModelFactory = checkoutModelFactory;
            _logger = logger;
            _taxService = taxService;
            _notificationService = notificationService;
            _paymentPluginManager = paymentPluginManager;
            _addressService = addressService;
            _discountService = discountService;
            _pictureService = pictureService;
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Configure()
        {
            //whether user has the authority
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            int storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            ZipMoneyPaymentSettings zipMoneyPaymentSettings =
                await _settingService.LoadSettingAsync<ZipMoneyPaymentSettings>(storeScope);

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
                    await _settingService.SettingExistsAsync(zipMoneyPaymentSettings, x => x.UseSandbox, storeScope);
                model.SandboxAPIKey_OverrideForStore =
                    await _settingService.SettingExistsAsync(zipMoneyPaymentSettings, x => x.SandboxAPIKey, storeScope);
                model.SandboxPublicKey_OverrideForStore =
                    await _settingService.SettingExistsAsync(zipMoneyPaymentSettings, x => x.SandboxPublicKey, storeScope);
                model.ProductionAPIKey_OverrideForStore =
                    await _settingService.SettingExistsAsync(zipMoneyPaymentSettings, x => x.ProductionAPIKey, storeScope);
                model.ProductionPublicKey_OverrideForStore = await _settingService.SettingExistsAsync(zipMoneyPaymentSettings,
                    x => x.ProductionPublicKey, storeScope);
            }

            return View("~/Plugins/Payments.ZipMoney/Views/Configure.cshtml", model);
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [AutoValidateAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            //whether user has the authority
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            //load settings for a chosen store scope
            int storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            ZipMoneyPaymentSettings zipMoneyPaymentSettings =
                await _settingService.LoadSettingAsync<ZipMoneyPaymentSettings>(storeScope);

            //save settings
            zipMoneyPaymentSettings.UseSandbox = model.UseSandbox;
            zipMoneyPaymentSettings.SandboxAPIKey = model.SandboxAPIKey;
            zipMoneyPaymentSettings.SandboxPublicKey = model.SandboxPublicKey;
            zipMoneyPaymentSettings.ProductionAPIKey = model.ProductionAPIKey;
            zipMoneyPaymentSettings.ProductionPublicKey = model.ProductionPublicKey;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            await _settingService.SaveSettingOverridablePerStoreAsync(zipMoneyPaymentSettings, x => x.UseSandbox,
                model.UseSandbox_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(zipMoneyPaymentSettings, x => x.SandboxAPIKey,
                model.SandboxAPIKey_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(zipMoneyPaymentSettings, x => x.SandboxPublicKey,
                model.SandboxPublicKey_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(zipMoneyPaymentSettings, x => x.ProductionAPIKey,
                model.ProductionAPIKey_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(zipMoneyPaymentSettings, x => x.ProductionPublicKey,
                model.ProductionPublicKey_OverrideForStore, storeScope, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> CancelOrder()
        {
            var cust = await _workContext.GetCurrentCustomerAsync();
            Order order = (await _orderService.SearchOrdersAsync((_storeContext.GetCurrentStoreAsync()).Id,
                customerId: cust.Id, pageSize: 1)).FirstOrDefault();
            if (order != null)
                return RedirectToRoute("OrderDetails", new {orderId = order.Id});

            return RedirectToRoute("HomePage");
        }

        [AllowAnonymous]
        public async Task<string> ZipCheckout()
        {
            int storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var cust = await _workContext.GetCurrentCustomerAsync();
            ZipMoneyPaymentSettings zipMoneyPaymentSettings =
                await _settingService.LoadSettingAsync<ZipMoneyPaymentSettings>(storeScope);
            ZipCheckoutRequest zipCheckout = new ZipCheckoutRequest();
            string apikey = zipMoneyPaymentSettings.UseSandbox
                ? zipMoneyPaymentSettings.SandboxAPIKey
                : zipMoneyPaymentSettings.ProductionAPIKey;
            PlaceOrderContainer details = await PreparePlaceOrderDetailsAsync();
            var orders = await _orderService.SearchOrdersAsync(customerId: cust.Id);
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
            var billcountry = await _countryService.GetCountryByAddressAsync(details.BillingAddress);
            var billstate = await _stateProvinceService.GetStateProvinceByAddressAsync(details.BillingAddress);
            zipCheckout.shopper = new ZipShopper
            {
                billing_address = new ZipAddress
                {
                    first_name = details.BillingAddress.FirstName,
                    last_name = details.BillingAddress.LastName,
                    line1 = details.BillingAddress.Address1,
                    line2 = details.BillingAddress.Address2,
                    city = details.BillingAddress.City,
                    country = billcountry != null ? billcountry.TwoLetterIsoCode : "AU",
                    postal_code = details.BillingAddress.ZipPostalCode,
                    state = billstate != null ? billstate.Name : ""
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
                    pickup = details.PickupInStore,
                },
                items = new List<ZipOrderItem>()
            };
            var shipcountry = await _countryService.GetCountryByAddressAsync(details.ShippingAddress);
            var shipstate = await _stateProvinceService.GetStateProvinceByAddressAsync(details.ShippingAddress);
            if (details.ShippingAddress != null)
            {
                zipCheckout.order.shipping.address = new ZipAddress
                {
                    line1 = details.ShippingAddress.Address1,
                    line2 = details.ShippingAddress.Address2,
                    first_name = details.ShippingAddress.FirstName,
                    last_name = details.ShippingAddress.LastName,
                    city = details.ShippingAddress.City,
                    country = shipcountry != null ? shipcountry.TwoLetterIsoCode : "AU",
                    state = shipstate != null ? shipstate.Name : "",
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
                var product = await _productService.GetProductByIdAsync(item.ProductId);
                ZipOrderItem zipOrderItem = new ZipOrderItem
                {
                    amount = product.Price,
                    name = product.Name,
                    quantity = item.Quantity,
                    type = OrderType.sku,
                    product_code = product.Sku,
                };

                string url = _webHelper.GetStoreLocation(null) + "content/images/";
                var itempic = await _pictureService.GetProductPictureAsync(product, item.AttributesXml);
                zipOrderItem.image_uri = (await _pictureService.GetPictureUrlAsync(itempic)).Url;
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
                redirect_uri = _webHelper.GetStoreLocation(null) + "PaymentZipMoney/ZipRedirect"
            };
            zipCheckout.metadata =
                new Dictionary<string, string> {{"CustomerId", cust.CustomerGuid.ToString()}};
            ZipMoneyProcessor zm = new ZipMoneyProcessor(apikey, zipMoneyPaymentSettings.UseSandbox);
            await _logger.InsertLogAsync(LogLevel.Debug,"Zip checkoutrequest",JsonConvert.SerializeObject(zipCheckout),cust);
            ZipCheckoutResponse zcr = await zm.CreateCheckout(zipCheckout);
            await _logger.InsertLogAsync(LogLevel.Debug,"zip checkoutresponse",zm.GetLastResponse(),cust);
            if (zm.GetLastError() != null)
            {
                await _logger.InsertLogAsync(LogLevel.Error, "zip error", zm.GetLastResponse());
            }
            await _genericAttributeService.SaveAttributeAsync(cust, "ZipCheckoutId", zcr.id);
            return JsonConvert.SerializeObject(zcr);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ZipRedirect(string result, string checkoutid)
        {
            var cust = await _workContext.GetCurrentCustomerAsync();
            await _logger.InsertLogAsync(LogLevel.Debug, "ZipRedirect called. " + result + ". " + checkoutid, "",
                cust);
            if (result == null) return null;
            if (checkoutid == null) return null;
            int storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            ZipMoneyPaymentSettings zipMoneyPaymentSettings =
                await _settingService.LoadSettingAsync<ZipMoneyPaymentSettings>(storeScope);
            string apikey = zipMoneyPaymentSettings.UseSandbox
                ? zipMoneyPaymentSettings.SandboxAPIKey
                : zipMoneyPaymentSettings.ProductionAPIKey;
            ZipMoneyProcessor zm = new ZipMoneyProcessor(apikey, zipMoneyPaymentSettings.UseSandbox);
            var checkoutr = zm.RetreiveCheckout(checkoutid).Result;
            if (checkoutr.metadata != null)
            {
                if (checkoutr.metadata.ContainsKey("CustomerId"))
                {
                    var custguid = checkoutr.metadata["CustomerId"];
                    var cc = await _customerService.GetCustomerByGuidAsync(Guid.Parse(custguid));
                    if (cc != null)
                    {
                        if (cc.CustomerGuid != cust.CustomerGuid)
                        {
                            await _logger.InsertLogAsync(LogLevel.Debug, "Different cust " + cc.CustomerGuid + " " + cust.CustomerGuid,"",
                cust);
                            await _workContext.SetCurrentCustomerAsync(cc);
                        }
                    }
                }
            }
            if (result.ToLowerInvariant().Equals("approved"))
            {
                string savedcheckoutId = await _genericAttributeService.GetAttributeAsync<string>(cust, "ZipCheckoutId");
                await _logger.InsertLogAsync(LogLevel.Debug, "saved checkoutid " + savedcheckoutId, savedcheckoutId, cust);
                if (savedcheckoutId != null && savedcheckoutId.Equals(checkoutid))
                {
//same session
                    PlaceOrderContainer details = await PreparePlaceOrderDetailsAsync();
                    decimal amount = details.OrderTotal;
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
                    await _logger.InsertLogAsync(LogLevel.Debug, "ZipCharge JSON", JsonConvert.SerializeObject(zipCharge),
                        cust);
                    ZipChargeResponse response = zm.CreateCharge(zipCharge).Result;
                    await _logger.InsertLogAsync(LogLevel.Debug, "ZipCharge Result JSON", zm.GetLastResponse(),
                        cust);
                    if (zm.GetLastError() != null)
                    {
                        await _logger.InsertLogAsync(LogLevel.Error, "ZipMoney Error",
                            zm.GetLastResponse(),
                            cust);
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
                            case "serialization":
                            default:
                                HttpContext.Session.SetString("ZipFriendlyError",
                                    "There was an error authorising your payment. Freerange Supplies did not receive a valid response from ZipMoney. Please try again or choose a different payment method");
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
                    return await EnterPaymentInfo(collection);
                }
                await _logger.InsertLogAsync(LogLevel.Debug, "Searching for checkoutid","", cust);
                //different session. most likely referral that is now approved
                var orders = (await _orderService.SearchOrdersAsync(paymentMethodSystemName: "Payments.ZipMoney"));
                var order = orders.FirstOrDefault(o => o.AuthorizationTransactionId == checkoutid);
                if (order != null)
                {
                    await _logger.InsertLogAsync(LogLevel.Debug, "Searching for checkoutid: found","", cust);
                    await _orderProcessingService.MarkAsAuthorizedAsync(order);
                    var errors = await _orderProcessingService.CaptureAsync(order);
                    if (order.PaymentStatus == PaymentStatus.Paid)
                        return RedirectToRoute("CheckoutCompleted", new {orderId = order.Id});
                    await _logger.InsertLogAsync(LogLevel.Error, "Could not capture transaction. Checkout_id=" + checkoutid,
                        errors.ToString());
                }
                else
                {
                    await _logger.InsertLogAsync(LogLevel.Error, "Order not found","", cust);
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
                return await EnterPaymentInfo(collection);
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

        public virtual async Task<IActionResult> EnterPaymentInfo(IFormCollection form)
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var cart = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), ShoppingCartType.ShoppingCart, (await _storeContext.GetCurrentStoreAsync()).Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (await _customerService.IsGuestAsync(await _workContext.GetCurrentCustomerAsync()) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            //Check whether payment workflow is required
            var isPaymentWorkflowRequired = await _orderProcessingService.IsPaymentWorkflowRequiredAsync(cart);
            if (!isPaymentWorkflowRequired)
            {
                return RedirectToRoute("CheckoutConfirm");
            }

            //load payment method
            var paymentMethodSystemName = await _genericAttributeService.GetAttributeAsync<string>(await _workContext.GetCurrentCustomerAsync(),
                NopCustomerDefaults.SelectedPaymentMethodAttribute, (await _storeContext.GetCurrentStoreAsync()).Id);
            var paymentMethod = await _paymentPluginManager
                .LoadPluginBySystemNameAsync(paymentMethodSystemName, await _workContext.GetCurrentCustomerAsync(), (await _storeContext.GetCurrentStoreAsync()).Id);
            if (paymentMethod == null)
                return RedirectToRoute("CheckoutPaymentMethod");

            var warnings = await paymentMethod.ValidatePaymentFormAsync(form);
            foreach (var warning in warnings)
                ModelState.AddModelError("", warning);
            if (ModelState.IsValid)
            {
                //get payment info
                var paymentInfo = await paymentMethod.GetPaymentInfoAsync(form);
                //set previous order GUID (if exists)
                _paymentService.GenerateOrderGuid(paymentInfo);

                //session save
                HttpContext.Session.Set("OrderPaymentInfo", paymentInfo);
                return RedirectToRoute("CheckoutConfirm");
            }

            //If we got this far, something failed, redisplay form
            //model
            var model = await _checkoutModelFactory.PreparePaymentInfoModelAsync(paymentMethod);
            return View(model);
        }

        /// <summary>
        /// Prepare details to place an order. It also sets some properties to "processPaymentRequest"
        /// </summary>
        /// <param name="processPaymentRequest">Process payment request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the details
        /// </returns>
        protected virtual async Task<PlaceOrderContainer> PreparePlaceOrderDetailsAsync()
        {
            var storeId = (await _storeContext.GetCurrentStoreAsync()).Id;
            var customerid = (await _workContext.GetCurrentCustomerAsync()).Id;
            var details = new PlaceOrderContainer
            {
                //customer
                Customer = await _customerService.GetCustomerByIdAsync(customerid)
            };
            if (details.Customer == null)
                throw new ArgumentException("Customer is not set");

            //affiliate
            var affiliate = await _affiliateService.GetAffiliateByIdAsync(details.Customer.AffiliateId);
            if (affiliate != null && affiliate.Active && !affiliate.Deleted)
                details.AffiliateId = affiliate.Id;

            //check whether customer is guest
            if (await _customerService.IsGuestAsync(details.Customer) && !_orderSettings.AnonymousCheckoutAllowed)
                throw new NopException("Anonymous checkout is not allowed");

            //customer currency
            var currencyTmp = await _currencyService.GetCurrencyByIdAsync(
                await _genericAttributeService.GetAttributeAsync<int>(details.Customer, NopCustomerDefaults.CurrencyIdAttribute, storeId));
            var customerCurrency = currencyTmp != null && currencyTmp.Published ? currencyTmp : await _workContext.GetWorkingCurrencyAsync();
            var primaryStoreCurrency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
            details.CustomerCurrencyCode = customerCurrency.CurrencyCode;
            details.CustomerCurrencyRate = customerCurrency.Rate / primaryStoreCurrency.Rate;

            //customer language
            details.CustomerLanguage = await _languageService.GetLanguageByIdAsync(
                await _genericAttributeService.GetAttributeAsync<int>(details.Customer, NopCustomerDefaults.LanguageIdAttribute, storeId));
            if (details.CustomerLanguage == null || !details.CustomerLanguage.Published)
                details.CustomerLanguage = await _workContext.GetWorkingLanguageAsync();

            //billing address
            if (details.Customer.BillingAddressId is null)
                throw new NopException("Billing address is not provided");

            var billingAddress = await _customerService.GetCustomerBillingAddressAsync(details.Customer);

            if (!CommonHelper.IsValidEmail(billingAddress?.Email))
                throw new NopException("Email is not valid");

            details.BillingAddress = _addressService.CloneAddress(billingAddress);

            if (await _countryService.GetCountryByAddressAsync(details.BillingAddress) is Country billingCountry && !billingCountry.AllowsBilling)
                throw new NopException($"Country '{billingCountry.Name}' is not allowed for billing");

            //checkout attributes
            details.CheckoutAttributesXml = await _genericAttributeService.GetAttributeAsync<string>(details.Customer, NopCustomerDefaults.CheckoutAttributes, storeId);
            details.CheckoutAttributeDescription = await _checkoutAttributeFormatter.FormatAttributesAsync(details.CheckoutAttributesXml, details.Customer);

            //load shopping cart
            details.Cart = await _shoppingCartService.GetShoppingCartAsync(details.Customer, ShoppingCartType.ShoppingCart, storeId);

            if (!details.Cart.Any())
                throw new NopException("Cart is empty");

            //validate the entire shopping cart
            var warnings = await _shoppingCartService.GetShoppingCartWarningsAsync(details.Cart, details.CheckoutAttributesXml, true);
            if (warnings.Any())
                throw new NopException(warnings.Aggregate(string.Empty, (current, next) => $"{current}{next};"));

            //validate individual cart items
            foreach (var sci in details.Cart)
            {
                var product = await _productService.GetProductByIdAsync(sci.ProductId);

                var sciWarnings = await _shoppingCartService.GetShoppingCartItemWarningsAsync(details.Customer,
                    sci.ShoppingCartType, product, storeId, sci.AttributesXml,
                    sci.CustomerEnteredPrice, sci.RentalStartDateUtc, sci.RentalEndDateUtc, sci.Quantity, false, sci.Id);
                if (sciWarnings.Any())
                    throw new NopException(sciWarnings.Aggregate(string.Empty, (current, next) => $"{current}{next};"));
            }

            //min totals validation
            if (!await ValidateMinOrderSubtotalAmountAsync(details.Cart))
            {
                var minOrderSubtotalAmount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(_orderSettings.MinOrderSubtotalAmount, await _workContext.GetWorkingCurrencyAsync());
                throw new NopException(string.Format(await _localizationService.GetResourceAsync("Checkout.MinOrderSubtotalAmount"),
                    await _priceFormatter.FormatPriceAsync(minOrderSubtotalAmount, true, false)));
            }

            if (!await ValidateMinOrderTotalAmountAsync(details.Cart))
            {
                var minOrderTotalAmount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(_orderSettings.MinOrderTotalAmount, await _workContext.GetWorkingCurrencyAsync());
                throw new NopException(string.Format(await _localizationService.GetResourceAsync("Checkout.MinOrderTotalAmount"),
                    await _priceFormatter.FormatPriceAsync(minOrderTotalAmount, true, false)));
            }

            //tax display type
            if (_taxSettings.AllowCustomersToSelectTaxDisplayType)
                details.CustomerTaxDisplayType = (TaxDisplayType)await _genericAttributeService.GetAttributeAsync<int>(details.Customer, NopCustomerDefaults.TaxDisplayTypeIdAttribute, storeId);
            else
                details.CustomerTaxDisplayType = _taxSettings.TaxDisplayType;

            //sub total (incl tax)
            var (orderSubTotalDiscountAmount, orderSubTotalAppliedDiscounts, subTotalWithoutDiscountBase, _, _) = await _orderTotalCalculationService.GetShoppingCartSubTotalAsync(details.Cart, true);
            details.OrderSubTotalInclTax = subTotalWithoutDiscountBase;
            details.OrderSubTotalDiscountInclTax = orderSubTotalDiscountAmount;

            //discount history
            foreach (var disc in orderSubTotalAppliedDiscounts)
                if (!_discountService.ContainsDiscount(details.AppliedDiscounts, disc))
                    details.AppliedDiscounts.Add(disc);

            //sub total (excl tax)
            (orderSubTotalDiscountAmount, _, subTotalWithoutDiscountBase, _, _) = await _orderTotalCalculationService.GetShoppingCartSubTotalAsync(details.Cart, false);
            details.OrderSubTotalExclTax = subTotalWithoutDiscountBase;
            details.OrderSubTotalDiscountExclTax = orderSubTotalDiscountAmount;

            //shipping info
            if (await _shoppingCartService.ShoppingCartRequiresShippingAsync(details.Cart))
            {
                var pickupPoint = await _genericAttributeService.GetAttributeAsync<PickupPoint>(details.Customer,
                    NopCustomerDefaults.SelectedPickupPointAttribute, storeId);
                if (_shippingSettings.AllowPickupInStore && pickupPoint != null)
                {
                    var country = await _countryService.GetCountryByTwoLetterIsoCodeAsync(pickupPoint.CountryCode);
                    var state = await _stateProvinceService.GetStateProvinceByAbbreviationAsync(pickupPoint.StateAbbreviation, country?.Id);

                    details.PickupInStore = true;
                    details.PickupAddress = new Address
                    {
                        Address1 = pickupPoint.Address,
                        City = pickupPoint.City,
                        County = pickupPoint.County,
                        CountryId = country?.Id,
                        StateProvinceId = state?.Id,
                        ZipPostalCode = pickupPoint.ZipPostalCode,
                        CreatedOnUtc = DateTime.UtcNow
                    };
                }
                else
                {
                    if (details.Customer.ShippingAddressId == null)
                        throw new NopException("Shipping address is not provided");

                    var shippingAddress = await _customerService.GetCustomerShippingAddressAsync(details.Customer);

                    if (!CommonHelper.IsValidEmail(shippingAddress?.Email))
                        throw new NopException("Email is not valid");

                    //clone shipping address
                    details.ShippingAddress = _addressService.CloneAddress(shippingAddress);

                    if (await _countryService.GetCountryByAddressAsync(details.ShippingAddress) is Country shippingCountry && !shippingCountry.AllowsShipping)
                        throw new NopException($"Country '{shippingCountry.Name}' is not allowed for shipping");
                }

                var shippingOption = await _genericAttributeService.GetAttributeAsync<ShippingOption>(details.Customer,
                    NopCustomerDefaults.SelectedShippingOptionAttribute, storeId);
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
            var (orderShippingTotalInclTax, _, shippingTotalDiscounts) = await _orderTotalCalculationService.GetShoppingCartShippingTotalAsync(details.Cart, true);
            var (orderShippingTotalExclTax, _, _) = await _orderTotalCalculationService.GetShoppingCartShippingTotalAsync(details.Cart, false);
            if (!orderShippingTotalInclTax.HasValue || !orderShippingTotalExclTax.HasValue)
                throw new NopException("Shipping total couldn't be calculated");

            details.OrderShippingTotalInclTax = orderShippingTotalInclTax.Value;
            details.OrderShippingTotalExclTax = orderShippingTotalExclTax.Value;

            foreach (var disc in shippingTotalDiscounts)
                if (!_discountService.ContainsDiscount(details.AppliedDiscounts, disc))
                    details.AppliedDiscounts.Add(disc);

            //payment total
            var paymentAdditionalFee = await _paymentService.GetAdditionalHandlingFeeAsync(details.Cart, "Payments.BrainTree");
            details.PaymentAdditionalFeeInclTax = (await _taxService.GetPaymentMethodAdditionalFeeAsync(paymentAdditionalFee, true, details.Customer)).price;
            details.PaymentAdditionalFeeExclTax = (await _taxService.GetPaymentMethodAdditionalFeeAsync(paymentAdditionalFee, false, details.Customer)).price;

            //tax amount
            SortedDictionary<decimal, decimal> taxRatesDictionary;
            (details.OrderTaxTotal, taxRatesDictionary) = await _orderTotalCalculationService.GetTaxTotalAsync(details.Cart);

            //VAT number
            var customerVatStatus = (VatNumberStatus)await _genericAttributeService.GetAttributeAsync<int>(details.Customer, NopCustomerDefaults.VatNumberStatusIdAttribute);
            if (_taxSettings.EuVatEnabled && customerVatStatus == VatNumberStatus.Valid)
                details.VatNumber = await _genericAttributeService.GetAttributeAsync<string>(details.Customer, NopCustomerDefaults.VatNumberAttribute);

            //tax rates
            details.TaxRates = taxRatesDictionary.Aggregate(string.Empty, (current, next) =>
                $"{current}{next.Key.ToString(CultureInfo.InvariantCulture)}:{next.Value.ToString(CultureInfo.InvariantCulture)};   ");

            //order total (and applied discounts, gift cards, reward points)
            var (orderTotal, orderDiscountAmount, orderAppliedDiscounts, appliedGiftCards, redeemedRewardPoints, redeemedRewardPointsAmount) = await _orderTotalCalculationService.GetShoppingCartTotalAsync(details.Cart);
            if (!orderTotal.HasValue)
                throw new NopException("Order total couldn't be calculated");

            details.OrderDiscountAmount = orderDiscountAmount;
            details.RedeemedRewardPoints = redeemedRewardPoints;
            details.RedeemedRewardPointsAmount = redeemedRewardPointsAmount;
            details.AppliedGiftCards = appliedGiftCards;
            details.OrderTotal = orderTotal.Value;

            //discount history
            foreach (var disc in orderAppliedDiscounts)
                if (!_discountService.ContainsDiscount(details.AppliedDiscounts, disc))
                    details.AppliedDiscounts.Add(disc);

            //recurring or standard shopping cart?
            details.IsRecurringShoppingCart = await _shoppingCartService.ShoppingCartIsRecurringAsync(details.Cart);
            if (!details.IsRecurringShoppingCart)
                return details;

            var (recurringCyclesError, recurringCycleLength, recurringCyclePeriod, recurringTotalCycles) = await _shoppingCartService.GetRecurringCycleInfoAsync(details.Cart);

            if (!string.IsNullOrEmpty(recurringCyclesError))
                throw new NopException(recurringCyclesError);

            return details;
        }

        /// <summary>
        /// Validate minimum order sub-total amount
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the rue - OK; false - minimum order sub-total amount is not reached
        /// </returns>
        public virtual async Task<bool> ValidateMinOrderSubtotalAmountAsync(IList<ShoppingCartItem> cart)
        {
            if (cart == null)
                throw new ArgumentNullException(nameof(cart));

            //min order amount sub-total validation
            if (!cart.Any() || _orderSettings.MinOrderSubtotalAmount <= decimal.Zero)
                return true;

            //subtotal
            var (_, _, subTotalWithoutDiscountBase, _, _) = await _orderTotalCalculationService.GetShoppingCartSubTotalAsync(cart, _orderSettings.MinOrderSubtotalAmountIncludingTax);

            if (subTotalWithoutDiscountBase < _orderSettings.MinOrderSubtotalAmount)
                return false;

            return true;
        }

        /// <summary>
        /// Validate minimum order total amount
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the rue - OK; false - minimum order total amount is not reached
        /// </returns>
        public virtual async Task<bool> ValidateMinOrderTotalAmountAsync(IList<ShoppingCartItem> cart)
        {
            if (cart == null)
                throw new ArgumentNullException(nameof(cart));

            if (!cart.Any() || _orderSettings.MinOrderTotalAmount <= decimal.Zero)
                return true;

            var shoppingCartTotalBase = (await _orderTotalCalculationService.GetShoppingCartTotalAsync(cart)).shoppingCartTotal;

            if (shoppingCartTotalBase.HasValue && shoppingCartTotalBase.Value < _orderSettings.MinOrderTotalAmount)
                return false;

            return true;
        }


        /// <summary>
        /// PlaceOrder container
        /// </summary>
        protected class PlaceOrderContainer
        {
            public PlaceOrderContainer()
            {
                Cart = new List<ShoppingCartItem>();
                AppliedDiscounts = new List<Discount>();
                AppliedGiftCards = new List<AppliedGiftCard>();
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
            public bool PickupInStore { get; set; }

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
            public List<Discount> AppliedDiscounts { get; set; }

            /// <summary>
            /// Applied gift cards
            /// </summary>
            public List<AppliedGiftCard> AppliedGiftCards { get; set; }

            /// <summary>
            /// Order subtotal (incl tax)
            /// </summary>
            public decimal OrderSubTotalInclTax { get; set; }

            /// <summary>
            /// Order subtotal (excl tax)
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