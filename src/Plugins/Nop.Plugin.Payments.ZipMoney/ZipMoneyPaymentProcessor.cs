using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Tax;
using ZipMoneySDK;
using ZipMoneySDK.Models;
using Nop.Services.Logging;
using Newtonsoft.Json;
using Nop.Core.Domain.Logging;
using Nop.Services.Plugins;

namespace Nop.Plugin.Payments.ZipMoney
{
    /// <inheritdoc />
    /// <summary>
    /// PayPalStandard payment processor
    /// </summary>
    public class ZipMoneyPaymentProcessor : BasePlugin, IPaymentMethod, IWidgetPlugin
    {
        #region Fields
        private readonly ZipMoneyPaymentSettings _zipMoneyPaymentSettings;
        private readonly ISettingService _settingService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly CurrencySettings _currencySettings;
        private readonly IWebHelper _webHelper;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ITaxService _taxService;
        private readonly ZipMoneyProcessor zipMoney;
        private readonly ILogger _logger;
        private readonly IWorkContext _workContext;
        #endregion

        #region Ctor

        public ZipMoneyPaymentProcessor(ZipMoneyPaymentSettings zipMoneyPaymentSettings,
            ISettingService settingService, ICurrencyService currencyService,ICustomerService customerService,
            CurrencySettings currencySettings, IWebHelper webHelper, ILogger logger, IWorkContext workContext,
            ICheckoutAttributeParser checkoutAttributeParser, ITaxService taxService)
        {
            this._zipMoneyPaymentSettings = zipMoneyPaymentSettings;
            this._settingService = settingService;
            this._currencyService = currencyService;
            this._customerService = customerService;
            this._currencySettings = currencySettings;
            this._webHelper = webHelper;
            this._checkoutAttributeParser = checkoutAttributeParser;
            this._taxService = taxService;
            _logger = logger;
            _workContext = workContext;
            string apikey = _zipMoneyPaymentSettings.UseSandbox
                ? _zipMoneyPaymentSettings.SandboxAPIKey
                : _zipMoneyPaymentSettings.ProductionAPIKey;
            zipMoney = new ZipMoneyProcessor(apikey, _zipMoneyPaymentSettings.UseSandbox);
        }

        #endregion

        public async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            ProcessPaymentResult result;
            var cust = await _workContext.GetCurrentCustomerAsync();
            await _logger.InsertLogAsync(LogLevel.Debug, "ZipMoney: Process Payment called", "", cust);
            if (processPaymentRequest.CustomValues["ZipCheckoutResult"].ToString().ToLowerInvariant().Equals("approved"))
            {
                ZipChargeResponse response = zipMoney
                    .CaptureCharge(processPaymentRequest.CustomValues["ZipChargeId"].ToString(),
                        processPaymentRequest.OrderTotal).Result;
                await _logger.InsertLogAsync(LogLevel.Debug, "ZipMoney: CaptureCharge", zipMoney.GetLastResponse(), cust);
                if (zipMoney.GetLastError() == null)
                {
                    if (response.state == ChargeState.captured)
                    {
                        result = new ProcessPaymentResult
                        {
                            AllowStoringCreditCardNumber = false,
                            CaptureTransactionId = response.id,
                            CaptureTransactionResult = response.state.ToString(),
                            NewPaymentStatus = PaymentStatus.Paid
                        };
                        return result;
                    }
                }
                await _logger.InsertLogAsync(LogLevel.Debug, "ZipMoney Capture Error",
                    JsonConvert.SerializeObject(zipMoney.GetLastError()), cust);
                await _logger.InsertLogAsync(LogLevel.Debug, "ZipMoney Capture Error Response",
                    zipMoney.GetLastResponse(), cust);
                return new ProcessPaymentResult
                {
                    AllowStoringCreditCardNumber = false,
                    AuthorizationTransactionId = processPaymentRequest.CustomValues["ZipCheckoutId"].ToString(),
                    AuthorizationTransactionResult = "authorised",
                    AuthorizationTransactionCode = processPaymentRequest.CustomValues["ZipChargeId"].ToString(),
                    NewPaymentStatus = PaymentStatus.Authorized
                };
            }
            else if (processPaymentRequest.CustomValues["ZipCheckoutResult"].ToString().ToLowerInvariant()
                .Equals("referred"))
            {
                return new ProcessPaymentResult
                {
                    AllowStoringCreditCardNumber = false,
                    AuthorizationTransactionId = processPaymentRequest.CustomValues["ZipCheckoutId"].ToString(),
                    AuthorizationTransactionResult = processPaymentRequest.CustomValues["ZipCheckoutResult"].ToString(),
                    AuthorizationTransactionCode = processPaymentRequest.CustomValues["ZipChargeId"].ToString(),
                    NewPaymentStatus = PaymentStatus.Pending
                };
            }
            result = new ProcessPaymentResult();
            result.AddError("Not valid checkout result");
            return result;
        }

        public Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //nothing
            return Task.CompletedTask;
        }

        public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return Task.FromResult(false);
        }

        public Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
        {
            return Task.FromResult((decimal)0);
        }

        public async Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            var cust = await _workContext.GetCurrentCustomerAsync();
            await _logger.InsertLogAsync(LogLevel.Debug, "ZipMoney: Capture called", "", cust);
            string chargeId = capturePaymentRequest.Order.AuthorizationTransactionCode;
            string chargeresult = capturePaymentRequest.Order.AuthorizationTransactionResult;
            CapturePaymentResult result;
            ZipChargeResponse response;
            if (chargeresult.ToLowerInvariant().Equals("authorised"))
            {
                response = zipMoney.CaptureCharge(chargeId, capturePaymentRequest.Order.OrderTotal).Result;
                await _logger.InsertLogAsync(LogLevel.Debug, "ZipMoney: Capture Charge", zipMoney.GetLastResponse(), cust);
                result = new CapturePaymentResult {NewPaymentStatus = PaymentStatus.Authorized};
                if (response?.state == ChargeState.captured)
                {
                    result.NewPaymentStatus = PaymentStatus.Paid;
                    result.CaptureTransactionId = response.id;
                    return result;
                }
                await _logger.InsertLogAsync(LogLevel.Error, "ZipMoney Capture Fail",
                    zipMoney.GetLastResponse(), cust);
                result.AddError("ZipMoney Capture Failed. Check log for more info");
                return result;
            }
            //create and capture
            ZipChargeRequest zipCharge = new ZipChargeRequest
            {
                authority = new ZipAuthority
                {
                    type =  AuthorityType.checkout_id,
                    value = capturePaymentRequest.Order.AuthorizationTransactionId
                },
                capture = true,
                amount = capturePaymentRequest.Order.OrderTotal,
                currency = "AUD"
            };
            await _logger.InsertLogAsync(LogLevel.Debug, "zip capture",
                JsonConvert.SerializeObject(zipCharge), cust);
            response = zipMoney.CreateCharge(zipCharge).Result;
            await _logger.InsertLogAsync(LogLevel.Debug, "zip capture response", zipMoney.GetLastResponse(), cust);
            result = new CapturePaymentResult();
            if (response?.state == ChargeState.captured)
            {
                result.NewPaymentStatus = PaymentStatus.Paid;
                result.CaptureTransactionId = response.id;
                result.CaptureTransactionResult = response.state.ToString();
                return result;
            }
            await _logger.InsertLogAsync(LogLevel.Error, "Zip capture/charge error",
                JsonConvert.SerializeObject(zipMoney.GetLastError()));
            result.Errors.Add(zipMoney.GetLastError().error.message);
            return result;
        }

        public async Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            var cust = await _workContext.GetCurrentCustomerAsync();
            RefundPaymentResult result = new RefundPaymentResult();
            var response = zipMoney.CreateRefund(refundPaymentRequest.Order.CaptureTransactionId, "plugin called refund",
                refundPaymentRequest.AmountToRefund).Result;
            var error = zipMoney.GetLastError();
            if (error == null)
            {
                result.NewPaymentStatus = refundPaymentRequest.IsPartialRefund
                    ? PaymentStatus.PartiallyRefunded
                    : PaymentStatus.Refunded;
                return result;
            }
            result.AddError(error.error.message);
            
            await _logger.InsertLogAsync(LogLevel.Debug, "zip refund response", zipMoney.GetLastResponse(), cust);
            await _logger.InsertLogAsync(LogLevel.Error, error.error.message, JsonConvert.SerializeObject(error));
            return result;
        }

        public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            return Task.FromResult(new VoidPaymentResult { Errors = new[] { "Void method not supported" } });
        }

        public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            return Task.FromResult(new ProcessPaymentResult());
        }

        public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            //always success
            return Task.FromResult(new CancelRecurringPaymentResult());
        }

        public Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //it's not a redirection payment method. So we always return false
            return Task.FromResult(false);
        }

        public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
        {
            var result =  new List<string>();
            if (!form.ContainsKey("ZipCheckoutId"))
                result.Add("No ZipMoney checkout was provided");
            if (!form.ContainsKey("ZipChargeId"))
                result.Add("No ZipMoney charge was provided");
            if (!form.ContainsKey("ZipCheckoutResult"))
                result.Add("Unknown ZipMoney decision");
            if (form["ZipCheckoutResult"] != StringValues.Empty)
            {
                var vals = form["ZipCheckoutResult"];
                if (vals.Contains("approved") || vals.Contains("referred"))
                {
                    
                }
                else
                {
                    result.Add("Does not contain valid ZipMoney result");
                }
            }
            return Task.FromResult<IList<string>>(result);
        }

        public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            ProcessPaymentRequest processPaymentRequest = new ProcessPaymentRequest();
            form.TryGetValue("ZipChargeId", out StringValues chargeid);
            form.TryGetValue("ZipCheckoutId", out StringValues checkoutid);
            form.TryGetValue("ZipCheckoutResult", out StringValues checkoutresult);
            processPaymentRequest.CustomValues["ZipChargeId"] = chargeid[0];
            processPaymentRequest.CustomValues["ZipCheckoutId"] = checkoutid[0];
            processPaymentRequest.CustomValues["ZipCheckoutResult"] = checkoutresult[0];
            return Task.FromResult(processPaymentRequest);
        }

        public string GetPublicViewComponentName()
        {
            return "PaymentZipMoney";
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentZipMoney/Configure";
        }

        public bool SupportCapture => true;

        public bool SupportPartiallyRefund => true;
        public bool SupportRefund => true;
        public bool SupportVoid => true;
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;
        public bool SkipPaymentInfo => false;

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        /// <remarks>
        /// return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
        /// for example, for a redirection payment method, description may be like this: "You will be redirected to PayPal site to complete the payment"
        /// </remarks>
        /// <returns>A task that represents the asynchronous operation</returns>
        public Task<string> GetPaymentMethodDescriptionAsync()
        {
            return Task.FromResult("Buy Now, Pay Later with ZipMoney");
        }

        public override async Task InstallAsync()
        {
            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            await base.UninstallAsync();
        }

        public bool HideInWidgetList { get; }

        public Task<IList<string>> GetWidgetZonesAsync()
        {
            return Task.FromResult<IList<string>>(new List<string>()
                {
                    "checkout_payment_method_top",
                    "order_summary_cart_footer",
                    "productdetails_inside_overview_buttons_before",
                    "checkout_confirm_top"
                }
            );
        }

        public string GetWidgetViewComponentName(string widgetZone)
        {
            if (widgetZone == "checkout_payment_method_top")
            {
                return "ZipMoneyInfo";
            }
            else if (widgetZone == "order_summary_cart_footer")
            {
                return "ZipMoneyCartPage";
            }
            else if (widgetZone == "productdetails_inside_overview_buttons_before")
            {
                return "ZipMoneyProductPage";
            }
            else if (widgetZone == "checkout_confirm_top")
            {
                return "ZipMoneyReferralConfirm";
            }

            return "";
        }
    }
}
