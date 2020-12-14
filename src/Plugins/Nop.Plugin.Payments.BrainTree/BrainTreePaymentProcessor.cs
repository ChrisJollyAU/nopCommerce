﻿using System;
using System.Collections.Generic;
using Braintree;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.BrainTree.Controllers;
using Nop.Plugin.Payments.BrainTree.Models;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Logging;
using Environment = Braintree.Environment;
using Nop.Services.Localization;
using Nop.Services.Plugins;

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
        private readonly IPaymentService _paymentService;
        private readonly BrainTreePaymentSettings _brainTreePaymentSettings;
        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;
        private readonly ILogger _logger;
        private readonly IOrderService _orderService;
        private readonly IAddressService _addressService;
        private readonly IStateProvinceService _stateProvinceService;
        #endregion

        #region Ctor

        public BrainTreePaymentProcessor(ICustomerService customerService,
            ISettingService settingService,
            BrainTreePaymentSettings brainTreePaymentSettings,
            IPaymentService paymentService,
            ILocalizationService localizationService,
            IOrderService orderService,
            IAddressService addressService,
            IStateProvinceService stateProvinceService,
            ILogger logger,
            IWebHelper webHelper)
        {
            this._customerService = customerService;
            this._settingService = settingService;
            _paymentService = paymentService;
            this._brainTreePaymentSettings = brainTreePaymentSettings;
            this._localizationService = localizationService;
            this._webHelper = webHelper;
            this._orderService = orderService;
            this._logger = logger;
            _addressService = addressService;
            _stateProvinceService = stateProvinceService;
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
            string device_data = "", nonce = "";
            if (processPaymentRequest.CustomValues.ContainsKey("nonce"))
            {
                nonce = (string)processPaymentRequest.CustomValues["nonce"];
                _logger.InsertLog(Core.Domain.Logging.LogLevel.Debug, "Braintree nonce", nonce, customer);
            }
            else
            {
                _logger.InsertLog(Core.Domain.Logging.LogLevel.Debug, "Braintree cannot find nonce", null, customer);
            }
            if (processPaymentRequest.CustomValues.ContainsKey("device_data"))
            {
                device_data = (string)processPaymentRequest.CustomValues["device_data"];
            }
            else
            {
                _logger.InsertLog(Core.Domain.Logging.LogLevel.Debug, "Braintree cannot find device_data", null, customer);
            }
            PaymentMethodNonce paymentMethodNonce = gateway.PaymentMethodNonce.Find(nonce);
            _logger.InsertLog(Core.Domain.Logging.LogLevel.Debug, "braintree nonce", paymentMethodNonce != null ? paymentMethodNonce.ToString() : "", customer);
            ThreeDSecureInfo info = paymentMethodNonce.ThreeDSecureInfo;
            if (paymentMethodNonce.Type == "CreditCard")
            {
                if (info == null || (info != null && info.LiabilityShifted == false))
                {
                    processPaymentResult.AddError("3D Secure not verified. Please go back to payment methods and try again");
                    _logger.InsertLog(Core.Domain.Logging.LogLevel.Error,"Braintree error. 3DS not provided or not shifted", null, customer);
                    return processPaymentResult;
                }
            }
            _logger.InsertLog(Core.Domain.Logging.LogLevel.Debug, "Braintree create transactionrequest", null, customer);
            //new transaction request
            var custbilladdress = _addressService.GetAddressById(customer.BillingAddressId.Value);
            var custshipaddress = _addressService.GetAddressById(customer.ShippingAddressId.Value);
            var transactionRequest = new TransactionRequest
            {
                Amount = processPaymentRequest.OrderTotal,
                OrderId = processPaymentRequest.OrderGuid.ToString(),
                Channel = BN_CODE,
                DeviceData = device_data,
                PaymentMethodNonce = nonce,
                Options = new TransactionOptionsRequest
                {
                    SubmitForSettlement = true
                },
                Customer = new CustomerRequest
                {
                    Company = custbilladdress.Company,
                    Email = customer.Email,
                    FirstName = custbilladdress.FirstName,
                    LastName = custbilladdress.LastName,
                    Phone = custbilladdress.PhoneNumber,
                    Fax = custbilladdress.FaxNumber
                }
            };
            _logger.InsertLog(Core.Domain.Logging.LogLevel.Debug, "Braintree create billingaddress", null, customer);
            try
            {
                var order = _orderService.GetOrderByGuid(processPaymentRequest.OrderGuid);
                if (order != null)
                {
                    var orderbilladdress = _addressService.GetAddressById(order.BillingAddressId);
                    if (orderbilladdress != null)
                    {
                        //address request
                        var addressRequest = new AddressRequest
                        {
                            FirstName = orderbilladdress.FirstName,
                            LastName = orderbilladdress.LastName,
                            StreetAddress = orderbilladdress.Address1,
                            PostalCode = orderbilladdress.ZipPostalCode,
                            Locality = orderbilladdress.City,
                            Company = orderbilladdress.Company,
                            Region = _stateProvinceService.GetStateProvinceById(orderbilladdress.StateProvinceId.Value).Name
                        };
                        transactionRequest.BillingAddress = addressRequest;
                    }
                    else
                    {
                        //address request
                        var addressRequest = new AddressRequest
                        {
                            FirstName = custbilladdress.FirstName,
                            LastName = custbilladdress.LastName,
                            StreetAddress = custbilladdress.Address1,
                            PostalCode = custbilladdress.ZipPostalCode,
                            Locality = custbilladdress.City,
                            Company = custbilladdress.Company,
                            Region = _stateProvinceService.GetStateProvinceById(custbilladdress.StateProvinceId.Value).Name
                        };
                        transactionRequest.BillingAddress = addressRequest;
                    }
                    _logger.InsertLog(Core.Domain.Logging.LogLevel.Debug, "Braintree create shippingrequest", null, customer);
                    if (!order.PickupInStore)
                    {
                        if (custshipaddress != null)
                        {
                            transactionRequest.ShippingAddress = new AddressRequest
                            {
                                FirstName = custshipaddress.FirstName,
                                LastName = custshipaddress.LastName,
                                StreetAddress = custshipaddress.Address1,
                                PostalCode = custshipaddress.ZipPostalCode,
                                Locality = custshipaddress.City,
                                Company = custshipaddress.Company,
                                Region = _stateProvinceService.GetStateProvinceById(custshipaddress.StateProvinceId.Value).Name
                            };
                        }
                        else
                        {
                            transactionRequest.ShippingAddress = transactionRequest.BillingAddress;
                        }
                    }
                    else
                    {
                        transactionRequest.ShippingAddress = transactionRequest.BillingAddress;
                    }
                }
                else
                {
                    _logger.InsertLog(Core.Domain.Logging.LogLevel.Debug, "Braintree order guid is null", null, customer);
                    //address request
                    var addressRequest = new AddressRequest
                    {
                        FirstName = custbilladdress.FirstName,
                        LastName = custbilladdress.LastName,
                        StreetAddress = custbilladdress.Address1,
                        PostalCode = custbilladdress.ZipPostalCode,
                        Locality = custbilladdress.City,
                        Company = custbilladdress.Company,
                        Region = _stateProvinceService.GetStateProvinceById(custbilladdress.StateProvinceId.Value).Name
                    };
                    transactionRequest.BillingAddress = addressRequest;
                    if (custshipaddress != null)
                    {
                        transactionRequest.ShippingAddress = new AddressRequest
                        {
                            FirstName = custshipaddress.FirstName,
                            LastName = custshipaddress.LastName,
                            StreetAddress = custshipaddress.Address1,
                            PostalCode = custshipaddress.ZipPostalCode,
                            Locality = custshipaddress.City,
                            Company = custshipaddress.Company,
                            Region = _stateProvinceService.GetStateProvinceById(custshipaddress.StateProvinceId.Value).Name
                        };
                    }
                    else
                    {
                        transactionRequest.ShippingAddress = transactionRequest.BillingAddress;
                    }
                }
            }
            catch (Exception e)
            {
                processPaymentResult.AddError(e.Message);
                return processPaymentResult;
            }
            
            _logger.InsertLog(Core.Domain.Logging.LogLevel.Debug, "Braintree create sale", null, customer);
            //sending a request
            var result = gateway.Transaction.Sale(transactionRequest);

            //result
            if (result.IsSuccess())
            {
                _logger.InsertLog(Core.Domain.Logging.LogLevel.Debug, "Braintree sale success", null, customer);
                processPaymentResult.NewPaymentStatus = PaymentStatus.Paid;
                processPaymentRequest.CustomValues["Braintree.Id"] = result.Target.Id;
                if (result.Target.RiskData != null)
                {
                    processPaymentRequest.CustomValues["RiskData.Id"] = result.Target.RiskData.id;
                    processPaymentRequest.CustomValues["RiskData.DeviceData"] = result.Target.RiskData.deviceDataCaptured;
                    processPaymentRequest.CustomValues["RiskData.Decision"] = result.Target.RiskData.decision;
                }
                if (result.Target.ThreeDSecureInfo != null)
                {
                    processPaymentRequest.CustomValues["Braintree.3DS.Enrolled"] = result.Target.ThreeDSecureInfo.Enrolled;
                    processPaymentRequest.CustomValues["Braintree.3DS.LiabilityShift"] = result.Target.ThreeDSecureInfo.LiabilityShifted;
                    processPaymentRequest.CustomValues["Braintree.3DS.LiabilityShiftPoss"] = result.Target.ThreeDSecureInfo.LiabilityShiftPossible;
                    processPaymentRequest.CustomValues["Braintree.3DS.Status"] = result.Target.ThreeDSecureInfo.Status;
                }
                if (result.Target.PayPalDetails != null)
                {
                    processPaymentRequest.CustomValues["Braintree.Paypal.PayerEmail"] = result.Target.PayPalDetails.PayerEmail;
                    processPaymentRequest.CustomValues["Braintree.Paypal.PayerFirstName"] = result.Target.PayPalDetails.PayerFirstName;
                    processPaymentRequest.CustomValues["Braintree.Paypal.PayerLastName"] = result.Target.PayPalDetails.PayerLastName;
                    processPaymentRequest.CustomValues["Braintree.Paypal.PayerStatus"] = result.Target.PayPalDetails.PayerStatus;
                    processPaymentRequest.CustomValues["Braintree.Paypal.SellerProtect"] = result.Target.PayPalDetails.SellerProtectionStatus;
                    processPaymentRequest.CustomValues["Braintree.Paypal.AuthId"] = result.Target.PayPalDetails.AuthorizationId;
                    processPaymentRequest.CustomValues["Braintree.Paypal.CaptureId"] = result.Target.PayPalDetails.CaptureId;
                }
            }
            else
            {
                processPaymentResult.AddError("Error processing payment." + result.Message);
                _logger.Error("Braintree error " + result.Message, null, customer);
                
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
            return _paymentService.CalculateAdditionalFee(cart,
                _brainTreePaymentSettings.AdditionalFee, _brainTreePaymentSettings.AdditionalFeePercentage);
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
            
            var trans = gateway.Transaction.Find((string)_paymentService.DeserializeCustomValues(refundPaymentRequest.Order)["Braintree.Id"]);
            if (trans.Status == TransactionStatus.SETTLED || trans.Status == TransactionStatus.SETTLING)
            {
                Result<Transaction> refundtrans;
                if (refundPaymentRequest.IsPartialRefund)
                {
                    refundtrans = gateway.Transaction.Refund(trans.Id,
                        refundPaymentRequest.AmountToRefund);
                }
                else
                {
                    refundtrans = gateway.Transaction.Refund(trans.Id);
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
            _logger.InsertLog(Core.Domain.Logging.LogLevel.Debug, "nonce get", form["nonce"][0]);
            return paymentInfo;
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
            _localizationService.AddPluginLocaleResource(new Dictionary<string, string>
            {
                ["Plugins.Payments.BrainTree.Fields.UseSandbox"] = "Use Sandbox",
                ["Plugins.Payments.BrainTree.Fields.UseSandbox.Hint"] =
                    "Check to enable Sandbox (testing environment).",
                ["Plugins.Payments.BrainTree.Fields.MerchantId"] = "Merchant ID",
                ["Plugins.Payments.BrainTree.Fields.MerchantId.Hint"] = "Enter Merchant ID",
                ["Plugins.Payments.BrainTree.Fields.PublicKey"] = "Public Key",
                ["Plugins.Payments.BrainTree.Fields.PublicKey.Hint"] = "Enter Public key",
                ["Plugins.Payments.BrainTree.Fields.PrivateKey"] = "Private Key",
                ["Plugins.Payments.BrainTree.Fields.PrivateKey.Hint"] = "Enter Private key",
                ["Plugins.Payments.BrainTree.Fields.AdditionalFee"] = "Additional fee",
                ["Plugins.Payments.BrainTree.Fields.AdditionalFee.Hint"] =
                    "Enter additional fee to charge your customers.",
                ["Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage"] = "Additional fee. Use percentage",
                ["Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage.Hint"] =
                    "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.",
                ["Plugins.Payments.BrainTree.PaymentMethodDescription"] = "Pay by credit / debit card"
            });
            

            base.Install();
        }

        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<BrainTreePaymentSettings>();

            //locales
            _localizationService.DeletePluginLocaleResources("Plugins.Payments.BrainTree");

            base.Uninstall();
        }

        public string GetPublicViewComponentName()
        {
            return "PaymentBrainTree";
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
