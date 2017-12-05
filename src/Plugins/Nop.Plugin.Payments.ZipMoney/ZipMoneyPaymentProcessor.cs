using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
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
        private ZipMoneyProcessor zipMoney;
        private readonly ILogger _logger;
        #endregion

        #region Ctor

        public ZipMoneyPaymentProcessor(ZipMoneyPaymentSettings zipMoneyPaymentSettings,
            ISettingService settingService, ICurrencyService currencyService,ICustomerService customerService,
            CurrencySettings currencySettings, IWebHelper webHelper, ILogger logger,
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
            string apikey = _zipMoneyPaymentSettings.UseSandbox
                ? _zipMoneyPaymentSettings.SandboxAPIKey
                : _zipMoneyPaymentSettings.ProductionAPIKey;
            zipMoney = new ZipMoneyProcessor(apikey, _zipMoneyPaymentSettings.UseSandbox);
        }

        #endregion

        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            ProcessPaymentResult result;
            if (processPaymentRequest.CustomValues["ZipCheckoutResult"].ToString().ToLowerInvariant().Equals("approved"))
            {
                ZipChargeResponse response = zipMoney
                    .CaptureCharge(processPaymentRequest.CustomValues["ZipChargeId"].ToString(),
                        processPaymentRequest.OrderTotal).Result;
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
                _logger.InsertLog(LogLevel.Debug, "ZipMoney Capture Error",
                    JsonConvert.SerializeObject(zipMoney.GetLastError()));
                _logger.InsertLog(LogLevel.Debug, "ZipMoney Capture Error Response",
                    zipMoney.GetLastResponse());
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

        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            return;
        }

        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            return false;
        }

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return 0;
        }

        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            string chargeId = capturePaymentRequest.Order.AuthorizationTransactionCode;
            string chargeresult = capturePaymentRequest.Order.AuthorizationTransactionResult;
            CapturePaymentResult result;
            ZipChargeResponse response;
            if (chargeresult.ToLowerInvariant().Equals("authorised"))
            {
                response = zipMoney.CaptureCharge(chargeId, capturePaymentRequest.Order.OrderTotal).Result;
                result = new CapturePaymentResult {NewPaymentStatus = PaymentStatus.Authorized};
                if (response.state == ChargeState.captured)
                {
                    result.NewPaymentStatus = PaymentStatus.Paid;
                    result.CaptureTransactionId = response.id;
                    return result;
                }
                _logger.InsertLog(LogLevel.Error, "ZipMoney Capture Fail",
                    zipMoney.GetLastResponse());
                result.AddError("ZipMoney Capture Failed. Check log for more info");
                return result;
            }
            //create and capture
            ZipChargeRequest zipCharge = new ZipChargeRequest
            {
                authority =
                {
                    type =  AuthorityType.checkout_id,
                    value = capturePaymentRequest.Order.AuthorizationTransactionId
                },
                capture = true,
                amount = capturePaymentRequest.Order.OrderTotal,
                currency = "AUD"
            };
            _logger.InsertLog(LogLevel.Debug, "zip capture",
                JsonConvert.SerializeObject(zipCharge));
            response = zipMoney.CreateCharge(zipCharge).Result;
            _logger.InsertLog(LogLevel.Debug, "zip capture response", zipMoney.GetLastResponse());
            result = new CapturePaymentResult();
            if (response.state == ChargeState.captured)
            {
                result.NewPaymentStatus = PaymentStatus.Paid;
                result.CaptureTransactionId = response.id;
                result.CaptureTransactionResult = response.state.ToString();
                return result;
            }
            _logger.InsertLog(LogLevel.Error, "Zip capture/charge error",
                JsonConvert.SerializeObject(zipMoney.GetLastError()));
            result.Errors.Add("Unknown error");
            return result;
        }

        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
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
            _logger.InsertLog(LogLevel.Error, error.error.message, JsonConvert.SerializeObject(error));
            return result;
        }

        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            return new VoidPaymentResult();
        }

        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult();
        }

        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return new CancelRecurringPaymentResult();
        }

        public bool CanRePostProcessPayment(Order order)
        {
            return false;
        }

        public IList<string> ValidatePaymentForm(IFormCollection form)
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
            return result;
        }

        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            ProcessPaymentRequest processPaymentRequest = new ProcessPaymentRequest();
            form.TryGetValue("ZipChargeId", out StringValues chargeid);
            form.TryGetValue("ZipCheckoutId", out StringValues checkoutid);
            form.TryGetValue("ZipCheckoutResult", out StringValues checkoutresult);
            processPaymentRequest.CustomValues["ZipChargeId"] = chargeid[0];
            processPaymentRequest.CustomValues["ZipCheckoutId"] = checkoutid[0];
            processPaymentRequest.CustomValues["ZipCheckoutResult"] = checkoutresult[0];
            return processPaymentRequest;
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentZipMoney/Configure";
        }

        public void GetPublicViewComponent(out string viewComponentName)
        {
            viewComponentName = "PaymentZipMoney";
        }

        public bool SupportCapture => true;

        public bool SupportPartiallyRefund => true;
        public bool SupportRefund => true;
        public bool SupportVoid => true;
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;
        public bool SkipPaymentInfo => false;
        public string PaymentMethodDescription => "Buy Now, Pay Later with ZipMoney";


        public override void Install()
        {
            base.Install();
        }

        public override void Uninstall()
        {
            base.Uninstall();
        }

        public IList<string> GetWidgetZones()
        {
            return new List<string>()
            {
                "checkout_payment_method_top",
                "order_summary_cart_footer",
                "productdetails_inside_overview_buttons_before",
                "checkout_confirm_top"
            };
        }

        public void GetPublicViewComponent(string widgetZone, out string viewComponentName)
        {
            viewComponentName = "";
            if (widgetZone == "checkout_payment_method_top")
            {
                viewComponentName = "ZipMoneyInfo";
            }
            else if (widgetZone == "order_summary_cart_footer")
            {
                viewComponentName = "ZipMoneyCartPage";
            }
            else if (widgetZone == "productdetails_inside_overview_buttons_before")
            {
                viewComponentName = "ZipMoneyProductPage";
            }
            else if (widgetZone == "checkout_confirm_top")
            {
                viewComponentName = "ZipMoneyReferralConfirm";
            }
        }
    }
}
