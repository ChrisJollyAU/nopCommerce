using System.Collections.Generic;
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
            ZipBaseResponse response = zipMoney.CaptureCharge(processPaymentRequest.CustomValues["ZipChargeId"].ToString(),processPaymentRequest.OrderTotal).Result;
            if (response.state == "captured")
            {
                ProcessPaymentResult result = new ProcessPaymentResult();
                if (response.state.ToLowerInvariant().Equals("captured"))
                {
                    result = new ProcessPaymentResult
                    {
                        AllowStoringCreditCardNumber = false,
                        AuthorizationTransactionId = processPaymentRequest.CustomValues["ZipChargeId"].ToString(),
                        CaptureTransactionId = response.id,
                        NewPaymentStatus = PaymentStatus.Paid
                    };
                }
                return result;
            }
            _logger.InsertLog(Core.Domain.Logging.LogLevel.Debug, "ZipMoney Capture Error",
                JsonConvert.SerializeObject(zipMoney.GetLastError()));
            _logger.InsertLog(Core.Domain.Logging.LogLevel.Debug, "ZipMoney Capture Error Response",
                JsonConvert.SerializeObject(response));
            return new ProcessPaymentResult
            {
                AllowStoringCreditCardNumber = false,
                AuthorizationTransactionId = processPaymentRequest.CustomValues["ZipChargeId"].ToString(),
                NewPaymentStatus = PaymentStatus.Authorized
            };
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
            string chargeId = capturePaymentRequest.Order.AuthorizationTransactionId;
            var response = zipMoney.CaptureCharge(chargeId, capturePaymentRequest.Order.OrderTotal).Result;
            CapturePaymentResult result = new CapturePaymentResult {NewPaymentStatus = PaymentStatus.Authorized};
            if (response.state == "captured")
            {
                result.NewPaymentStatus = PaymentStatus.Paid;
                result.CaptureTransactionId = response.id;
                return result;
            }
            _logger.InsertLog(Core.Domain.Logging.LogLevel.Debug, "ZipMoney Capture Fail",
                JsonConvert.SerializeObject(zipMoney.GetLastError()));
            result.AddError("ZipMoney Capture Failed. Check log for more info");
            return result;
        }

        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            RefundPaymentResult result = new RefundPaymentResult();
            var response = zipMoney.CreateRefund(refundPaymentRequest.Order.CaptureTransactionId, "",
                refundPaymentRequest.AmountToRefund).Result;
            result.NewPaymentStatus = refundPaymentRequest.IsPartialRefund
                ? PaymentStatus.PartiallyRefunded
                : PaymentStatus.Refunded;
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
            return result;
        }

        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            ProcessPaymentRequest processPaymentRequest = new ProcessPaymentRequest();
            form.TryGetValue("ZipChargeId", out StringValues chargeid);
            form.TryGetValue("ZipChargeId", out StringValues checkoutid);
            processPaymentRequest.CustomValues["ZipChargeId"] = chargeid[0];
            processPaymentRequest.CustomValues["ZipCheckoutId"] = checkoutid[0];
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
        public string PaymentMethodDescription => "ZipMoney";


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
                "productdetails_inside_overview_buttons_before"
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
        }
    }
}
