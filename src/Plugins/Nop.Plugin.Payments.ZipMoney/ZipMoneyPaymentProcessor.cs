using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Plugins;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Tax;

namespace Nop.Plugin.Payments.ZipMoney
{
    /// <summary>
    /// PayPalStandard payment processor
    /// </summary>
    public class ZipMoneyPaymentProcessor : BasePlugin, IPaymentMethod
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
        #endregion

        #region Ctor

        public ZipMoneyPaymentProcessor(ZipMoneyPaymentSettings zipMoneyPaymentSettings,
            ISettingService settingService, ICurrencyService currencyService,ICustomerService customerService,
            CurrencySettings currencySettings, IWebHelper webHelper,
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
        }

        #endregion

        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            processPaymentRequest.
            throw new System.NotImplementedException();
        }

        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            throw new System.NotImplementedException();
        }

        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            return false;
        }

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            throw new System.NotImplementedException();
        }

        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            throw new System.NotImplementedException();
        }

        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            throw new System.NotImplementedException();
        }

        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            throw new System.NotImplementedException();
        }

        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            throw new System.NotImplementedException();
        }

        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            throw new System.NotImplementedException();
        }

        public bool CanRePostProcessPayment(Order order)
        {
            throw new System.NotImplementedException();
        }

        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            throw new System.NotImplementedException();
        }

        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            throw new System.NotImplementedException();
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
        public bool SkipPaymentInfo { get; }
        public string PaymentMethodDescription { get; }
    }
}
