using System;
using System.Collections.Generic;
using Braintree;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.BrainTree.Controllers;
using Nop.Plugin.Payments.BrainTree.Models;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Environment = Braintree.Environment;
using Nop.Services.Localization;

namespace Nop.Plugin.Payments.BrainTree
{
    public class BrainTreePaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Constants

        /// <summary>
        /// nopCommerce partner code
        /// </summary>
        private const string BN_CODE = "nopCommerce_SP";

        #endregion

        #region Fields

        private readonly ICustomerService _customerService;
        private readonly ISettingService _settingService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly BrainTreePaymentSettings _brainTreePaymentSettings;
        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;

        #endregion

        #region Ctor

        public BrainTreePaymentProcessor(ICustomerService customerService,
            ISettingService settingService,
            IOrderTotalCalculationService orderTotalCalculationService,
            BrainTreePaymentSettings brainTreePaymentSettings,
            ILocalizationService localizationService,
            IWebHelper webHelper)
        {
            this._customerService = customerService;
            this._settingService = settingService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._brainTreePaymentSettings = brainTreePaymentSettings;
            this._localizationService = localizationService;
            this._webHelper = webHelper;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var processPaymentResult = new ProcessPaymentResult();

            //get customer
            var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);
            //get settings
            var useSandBox = _brainTreePaymentSettings.UseSandBox;
            var merchantId = _brainTreePaymentSettings.MerchantId;
            var publicKey = _brainTreePaymentSettings.PublicKey;
            var privateKey = _brainTreePaymentSettings.PrivateKey;

            //new gateway
            var gateway = new BraintreeGateway
            {
                Environment = useSandBox ? Environment.SANDBOX : Environment.PRODUCTION,
                MerchantId = merchantId,
                PublicKey = publicKey,
                PrivateKey = privateKey
            };

            //new transaction request
            var transactionRequest = new TransactionRequest
            {
                Amount = processPaymentRequest.OrderTotal,
                OrderId = processPaymentRequest.OrderGuid.ToString(),
                Channel = BN_CODE,
                DeviceData = (string)processPaymentRequest.CustomValues["device_data"],
                PaymentMethodNonce = (string)processPaymentRequest.CustomValues["nonce"],
                Options = new TransactionOptionsRequest
                {
                    SubmitForSettlement = true
                },
                Customer = new CustomerRequest
                {
                    Company = customer.BillingAddress.Company,
                    Email = customer.Email,
                    FirstName = customer.BillingAddress.FirstName,
                    LastName = customer.BillingAddress.LastName,
                    Phone = customer.BillingAddress.PhoneNumber,
                    Fax = customer.BillingAddress.FaxNumber
                }
            };

            //address request
            var addressRequest = new AddressRequest
            {
                FirstName = customer.BillingAddress.FirstName,
                LastName = customer.BillingAddress.LastName,
                StreetAddress = customer.BillingAddress.Address1,
                PostalCode = customer.BillingAddress.ZipPostalCode,
                Locality = customer.BillingAddress.City,
                Company = customer.BillingAddress.Company,
                Region = customer.BillingAddress.StateProvince.Name
            };
            transactionRequest.BillingAddress = addressRequest;
            transactionRequest.ShippingAddress = new AddressRequest
            {
                FirstName = customer.ShippingAddress.FirstName,
                LastName = customer.ShippingAddress.LastName,
                StreetAddress = customer.ShippingAddress.Address1,
                PostalCode = customer.ShippingAddress.ZipPostalCode,
                Locality = customer.ShippingAddress.City,
                Company = customer.ShippingAddress.Company,
                Region = customer.ShippingAddress.StateProvince.Name
            };

            //sending a request
            var result = gateway.Transaction.Sale(transactionRequest);

            //result
            if (result.IsSuccess())
            {
                processPaymentResult.NewPaymentStatus = PaymentStatus.Paid;
                processPaymentRequest.CustomValues["RiskData.Id"] = result.Target.RiskData.id;
                processPaymentRequest.CustomValues["RiskData.DeviceData"] = result.Target.RiskData.deviceDataCaptured;
                processPaymentRequest.CustomValues["RiskData.Decision"] = result.Target.RiskData.decision;
                processPaymentRequest.CustomValues["Braintree.Id"] = result.Target.Id;
            }
            else
            {
                processPaymentResult.AddError("Error processing payment." + result.Message);
            }

            return processPaymentResult;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //nothing
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
                _brainTreePaymentSettings.AdditionalFee, _brainTreePaymentSettings.AdditionalFeePercentage);
            return result;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            var gateway = new BraintreeGateway
            {
                Environment = _brainTreePaymentSettings.UseSandBox ? Environment.SANDBOX : Environment.PRODUCTION,
                MerchantId = _brainTreePaymentSettings.MerchantId,
                PublicKey = _brainTreePaymentSettings.PublicKey,
                PrivateKey = _brainTreePaymentSettings.PrivateKey
            };
            var trans = gateway.Transaction.Find((string)refundPaymentRequest.Order.DeserializeCustomValues()["Braintree.Id"]);
            if (trans.Status == TransactionStatus.SETTLED || trans.Status == TransactionStatus.SETTLING)
            {
                Result<Transaction> refundtrans;
                if (refundPaymentRequest.IsPartialRefund)
                {
                    refundtrans = gateway.Transaction.Refund(refundPaymentRequest.Order.OrderGuid.ToString(),
                        refundPaymentRequest.AmountToRefund);
                }
                else
                {
                    refundtrans = gateway.Transaction.Refund(refundPaymentRequest.Order.OrderGuid.ToString());
                }

                if (!refundtrans.IsSuccess())
                {
                    result.AddError(refundtrans.Message);
                }
                else
                {
                    result.NewPaymentStatus = refundPaymentRequest.IsPartialRefund
                        ? PaymentStatus.PartiallyRefunded
                        : PaymentStatus.Refunded;
                }
            }
            else
            {
                result.AddError("Transaction not available for refund. Try void");
            }
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //it's not a redirection payment method. So we always return false
            return false;
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentBrainTree/Configure";
        }

        

        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            paymentInfo.CustomValues["device_data"] = form["device_data"][0];
            paymentInfo.CustomValues["nonce"] = form["nonce"][0];
            return paymentInfo;
        }

        public void GetPublicViewComponent(out string viewComponentName)
        {
            viewComponentName = "PaymentBrainTree";
        }

        public Type GetControllerType()
        {
            return typeof(PaymentBrainTreeController);
        }

        public override void Install()
        {
            //settings
            var settings = new BrainTreePaymentSettings
            {
                UseSandBox = true,
                MerchantId = "",
                PrivateKey = "",
                PublicKey = ""
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseSandbox", "Use Sandbox");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseSandbox.Hint", "Check to enable Sandbox (testing environment).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.MerchantId", "Merchant ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.MerchantId.Hint", "Enter Merchant ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PublicKey", "Public Key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PublicKey.Hint", "Enter Public key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PrivateKey", "Private Key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PrivateKey.Hint", "Enter Private key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage", "Additional fee. Use percentage");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.PaymentMethodDescription", "Pay by credit / debit card");

            base.Install();
        }

        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<BrainTreePaymentSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseSandbox");
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseSandbox.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.MerchantId");
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.MerchantId.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PublicKey");
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PublicKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PrivateKey");
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PrivateKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage");
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.PaymentMethodDescription");

            base.Uninstall();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return RecurringPaymentType.NotSupported;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Standard;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription
        {
            get { return _localizationService.GetResource("Plugins.Payments.BrainTree.PaymentMethodDescription"); }
        }

        #endregion

    }
}
