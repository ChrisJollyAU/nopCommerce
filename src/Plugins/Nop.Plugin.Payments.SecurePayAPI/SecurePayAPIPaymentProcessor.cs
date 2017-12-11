using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.SecurePayAPI.Controllers;
using Nop.Plugin.Payments.SecurePayAPI.Models;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Tax;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Nop.Plugin.Payments.SecurePayAPI.Validators;

namespace Nop.Plugin.Payments.SecurePayAPI
{
    /// <summary>
    /// PayPalStandard payment processor
    /// </summary>
    public class SecurePayAPIPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields
        private readonly SecurePayAPIPaymentSettings _paypalStandardPaymentSettings;
        private readonly ISettingService _settingService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly CurrencySettings _currencySettings;
        private readonly IWebHelper _webHelper;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ITaxService _taxService;
        private readonly ILocalizationService _localizationService;

        private const string SecurePay_Url_Test = "https://test.securepay.com.au/xmlapi/payment";
        private const string SecurePay_Url_Live = "https://www.securepay.com.au/xmlapi/payment";
        private const string SecurePay_Url_PeriodicTest = "https://test.securepay.com.au/xmlapi/periodic";
        private const string SecurePay_Url_PeriodicLive = "https://www.securepay.com.au/xmlapi/periodic";
        private const string SecurePay_Url_FraudTest = "https://test.securepay.com.au/antifraud/payment";
        private const string SecurePay_Url_FraudLive = "https://www.securepay.com.au/antifraud/payment";

        private const int Txn_Standard = 0;
        private const int Txn_Periodic = 3;
        private const int Txn_Refund = 4;
        private const int Txn_Reverse = 6;
        private const int Txn_Preauth = 10;
        private const int Txn_Advice = 11;
        private const int Txn_Recurring = 14;
        private const int Txn_DirectDebit = 15;
        private const int Txn_DirectCredit = 17;
        private const int Txn_AntifraudPay = 21;
        private const int Txn_AntifraudCheck = 22;
        #endregion

        #region Ctor

        public SecurePayAPIPaymentProcessor(SecurePayAPIPaymentSettings paypalStandardPaymentSettings,
            ISettingService settingService, ICurrencyService currencyService,ICustomerService customerService,
            CurrencySettings currencySettings, IWebHelper webHelper, ILocalizationService localizationService,
            ICheckoutAttributeParser checkoutAttributeParser, ITaxService taxService)
        {
            this._paypalStandardPaymentSettings = paypalStandardPaymentSettings;
            this._settingService = settingService;
            this._currencyService = currencyService;
            this._customerService = customerService;
            this._currencySettings = currencySettings;
            this._webHelper = webHelper;
            this._checkoutAttributeParser = checkoutAttributeParser;
            this._taxService = taxService;
            _localizationService = localizationService;
        }

        #endregion

        #region Utilities
        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <returns>Additional handling fee</returns>
        /// 

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return 0;
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
            var result = new ProcessPaymentResult
            {
                AllowStoringCreditCardNumber = false,
                NewPaymentStatus = PaymentStatus.Pending
            };
            var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);
            var sp = new SPMessage.SecurePayMessage
            {
                MerchantInfo = new SPMessage.MerchantInfo
                {
                    merchantID = _paypalStandardPaymentSettings.MerchantId,
                    password = _paypalStandardPaymentSettings.Password
                },
                RequestType = "Payment",
                MessageInfo = new SPMessage.MessageInfo
                {
                    apiVersion = "xml-4.2",
                    timeoutValue = "60",
                    messageTimestamp = GetTimestamp(),
                    messageID = processPaymentRequest.OrderGuid + DateTime.UtcNow.Hour.ToString() + DateTime.UtcNow.Minute.ToString() + DateTime.UtcNow.Second.ToString() + DateTime.UtcNow.Millisecond.ToString()
                },
                Payment = new SPMessage.Payment()
            };
            sp.Payment.TxnList = new SPMessage.TxnList
            {
                count = "1",
                Txn = new SPMessage.Txn
                {
                    amount = ((int) (processPaymentRequest.OrderTotal * 100)).ToString(),
                    CreditCardInfo = new SPMessage.CreditCardInfo(),
                    ID = "1",
                    currency = "AUD",
                    purchaseOrderNo = processPaymentRequest.OrderGuid.ToString(),
                    txnSource = "23"
                }
            };
            if (_paypalStandardPaymentSettings.FraudGuard) {
                sp.Payment.TxnList.Txn.txnType = "21";
                string name = customer.GetFullName();
                string firstname = name.Substring(0,name.IndexOf(" "));
                string lastname = name.Substring(name.IndexOf(" ")+1);
                sp.Payment.TxnList.Txn.BuyerInfo = new SPMessage.BuyerInfo
                {
                    firstName = firstname,
                    lastName = lastname,
                    ip = customer.LastIpAddress
                };
            }
            else {
                if (_paypalStandardPaymentSettings.UsePreauth)
                {
                    sp.Payment.TxnList.Txn.txnType = "10";
                }
                else
                {
                    sp.Payment.TxnList.Txn.txnType = "0";
                }
            }
            sp.Payment.TxnList.Txn.CreditCardInfo.cardNumber = processPaymentRequest.CreditCardNumber;
            sp.Payment.TxnList.Txn.CreditCardInfo.cvv = processPaymentRequest.CreditCardCvv2;
            sp.Payment.TxnList.Txn.CreditCardInfo.expiryDate = (processPaymentRequest.CreditCardExpireMonth < 10 ? "0" + processPaymentRequest.CreditCardExpireMonth.ToString() : processPaymentRequest.CreditCardExpireMonth.ToString()) + "/" + processPaymentRequest.CreditCardExpireYear.ToString().Substring(2,2);
            
            XmlSerializer serializer = new XmlSerializer(typeof(SPMessage.SecurePayMessage));
            StringWriter stringWriter = new StringWriter();
            serializer.Serialize(stringWriter, sp);
            string message = stringWriter.ToString();
            //message = HttpUtility.UrlEncode(message);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(GetSecurePayAPIUrl(_paypalStandardPaymentSettings.FraudGuard,_paypalStandardPaymentSettings.TestAccount));
            request.ContentType = "text/xml";
            request.Method = "POST";
            StreamWriter sw = new StreamWriter(request.GetRequestStream());
            sw.Write(message);
            sw.Close();
            WebResponse response = request.GetResponse();
            
            string responsefromserver = String.Empty;
            if (response != null) {
                StreamReader sr = new StreamReader(response.GetResponseStream());
                responsefromserver = sr.ReadToEnd();
                //responsefromserver = HttpUtility.UrlDecode(responsefromserver);
                XmlSerializer ser = new XmlSerializer(typeof(SecurePayResponse.SecurePayMessage));
                StringReader str = new StringReader(responsefromserver);
                SecurePayResponse.SecurePayMessage msg = (SecurePayResponse.SecurePayMessage)ser.Deserialize(str);
                if (msg.Status.statusCode == "000")
                {
                    if (msg.Payment.TxnList.Txn[0].responseCode == "00" || msg.Payment.TxnList.Txn[0].responseCode == "08" || msg.Payment.TxnList.Txn[0].responseCode == "11" || msg.Payment.TxnList.Txn[0].responseCode == "16" || msg.Payment.TxnList.Txn[0].responseCode == "77")
                    {
                        processPaymentRequest.CreditCardCvv2 = null;
                        processPaymentRequest.CreditCardExpireMonth = 0;
                        processPaymentRequest.CreditCardExpireYear = 0;
                        processPaymentRequest.CreditCardName = null;
                        processPaymentRequest.CreditCardNumber = null;
                        processPaymentRequest.CreditCardType = null;
                        if (_paypalStandardPaymentSettings.UsePreauth)
                        {
                            result.NewPaymentStatus = PaymentStatus.Authorized;
                            result.AuthorizationTransactionId = msg.Payment.TxnList.Txn[0].preauthID;
                            result.AuthorizationTransactionResult = msg.Payment.TxnList.Txn[0].responseCode + msg.Payment.TxnList.Txn[0].responseText;
                        }
                        else
                        {
                            result.NewPaymentStatus = PaymentStatus.Paid;
                            result.CaptureTransactionId = msg.Payment.TxnList.Txn[0].txnID;
                            result.CaptureTransactionResult = msg.Payment.TxnList.Txn[0].responseCode + " " + msg.Payment.TxnList.Txn[0].responseText;
                        }
                    }
                    else
                    {
                        result.AddError(msg.Payment.TxnList.Txn[0].responseCode + " " + msg.Payment.TxnList.Txn[0].responseText);
                    }
                }
                else
                {
                    result.AddError("1 " + msg.Status.statusCode + " " + msg.Status.statusDescription);
                }
            }
            return result;
        }

        private string GetTimestamp()
        {
            TimeSpan ts = DateTime.Now - DateTime.UtcNow;
            int i = (int)Math.Round(ts.TotalMinutes,0);
            string sign = "";
            if (i < 0)
            {
                sign = "-";
                i = -1 * i;
            }
            else
            {
                sign = "+";
            }
            string buffer = "";
            if (i > -100 && i < 100)
            {
                buffer += "0";
            }
            if (i > -10 && i < 10)
            {
                buffer += "0";
            }
            return DateTime.Now.ToString("yyyyddMMHHmmssfff") + "000" + sign + buffer + i;
        }

        private string GetSecurePayAPIUrl(bool FraudGuard, bool TestAccount)
        {
            if (FraudGuard)
            {
                if (TestAccount)
                {
                    return SecurePay_Url_FraudTest;
                }
                else
                {
                    return SecurePay_Url_FraudLive;
                }
            }
            else
            {
                if (TestAccount)
                {
                    return SecurePay_Url_Test;
                }
                else
                {
                    return SecurePay_Url_Live;
                }
            }
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            var sp = new SPMessage.SecurePayMessage
            {
                MerchantInfo = new SPMessage.MerchantInfo()
            };
            sp.MerchantInfo.merchantID = _paypalStandardPaymentSettings.MerchantId;
            sp.MerchantInfo.password = _paypalStandardPaymentSettings.Password;
            sp.RequestType = "Payment";
            sp.MessageInfo = new SPMessage.MessageInfo
            {
                apiVersion = "xml-4.2",
                timeoutValue = "60",
                messageID = capturePaymentRequest.Order.OrderGuid + DateTime.UtcNow.Hour.ToString() + DateTime.UtcNow.Minute.ToString() + DateTime.UtcNow.Second.ToString() + DateTime.UtcNow.Millisecond.ToString()
            };
            sp.Payment = new SPMessage.Payment
            {
                TxnList = new SPMessage.TxnList
                {
                    count = "1",
                    Txn = new SPMessage.Txn
                    {
                        amount = ((int)(capturePaymentRequest.Order.OrderTotal * 100)).ToString(),
                        ID = "1",
                        currency = "AUD",
                        purchaseOrderNo = capturePaymentRequest.Order.OrderGuid.ToString(),
                        txnSource = "23",
                        txnType = "11",
                        preauthID = capturePaymentRequest.Order.AuthorizationTransactionId
                    }
                }
            };

            XmlSerializer serializer = new XmlSerializer(typeof(SPMessage.SecurePayMessage));
            StringWriter stringWriter = new StringWriter();
            serializer.Serialize(stringWriter, sp);
            string message = stringWriter.ToString();
            //message = HttpUtility.UrlEncode(message);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(GetSecurePayAPIUrl(false, _paypalStandardPaymentSettings.TestAccount));
            request.ContentType = "text/xml";
            request.Method = "POST";
            StreamWriter sw = new StreamWriter(request.GetRequestStream());
            sw.Write(message);
            sw.Close();
            WebResponse response = request.GetResponse();
            string responsefromserver = String.Empty;
            if (response != null)
            {
                StreamReader sr = new StreamReader(response.GetResponseStream());
                responsefromserver = sr.ReadToEnd();
                //responsefromserver = HttpUtility.UrlDecode(responsefromserver);
                XmlSerializer ser = new XmlSerializer(typeof(SecurePayResponse.SecurePayMessage));
                StringReader str = new StringReader(responsefromserver);
                SecurePayResponse.SecurePayMessage msg = (SecurePayResponse.SecurePayMessage)ser.Deserialize(str);
                if (msg.Status.statusCode == "000")
                {
                    result.NewPaymentStatus = PaymentStatus.Paid;
                    result.CaptureTransactionId = msg.Payment.TxnList.Txn[0].txnID;
                    result.CaptureTransactionResult = msg.Payment.TxnList.Txn[0].responseCode + " " +  msg.Payment.TxnList.Txn[0].responseText;
                }
                else
                {
                    result.AddError(msg.Status.statusCode + " " + msg.Status.statusDescription);
                }
            }
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
            var sp = new SPMessage.SecurePayMessage
            {
                MerchantInfo = new SPMessage.MerchantInfo
                {
                    merchantID = _paypalStandardPaymentSettings.MerchantId,
                    password = _paypalStandardPaymentSettings.Password
                },
                RequestType = "Payment",
                MessageInfo = new SPMessage.MessageInfo
                {
                    apiVersion = "xml-4.2",
                    timeoutValue = "60",
                    messageID = refundPaymentRequest.Order.OrderGuid + DateTime.UtcNow.Hour.ToString() + DateTime.UtcNow.Minute.ToString() + DateTime.UtcNow.Second.ToString() + DateTime.UtcNow.Millisecond.ToString()
                },
                Payment = new SPMessage.Payment
                {
                    TxnList = new SPMessage.TxnList
                    {
                        count = "1",
                        Txn = new SPMessage.Txn
                        {
                            amount = ((int)(refundPaymentRequest.AmountToRefund * 100)).ToString(),
                            ID = "1",
                            currency = "AUD",
                            purchaseOrderNo = refundPaymentRequest.Order.OrderGuid.ToString(),
                            txnSource = "23",
                            txnType = "4",
                            txnID = refundPaymentRequest.Order.CaptureTransactionId
                        }
                    }
                }
            };

            XmlSerializer serializer = new XmlSerializer(typeof(SPMessage.SecurePayMessage));
            StringWriter stringWriter = new StringWriter();
            serializer.Serialize(stringWriter, sp);
            string message = stringWriter.ToString();
            //message = HttpUtility.UrlEncode(message);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(GetSecurePayAPIUrl(false, _paypalStandardPaymentSettings.TestAccount));
            request.ContentType = "text/xml";
            request.Method = "POST";
            StreamWriter sw = new StreamWriter(request.GetRequestStream());
            sw.Write(message);
            sw.Close();
            WebResponse response = request.GetResponse();
            string responsefromserver = String.Empty;
            if (response != null)
            {
                StreamReader sr = new StreamReader(response.GetResponseStream());
                responsefromserver = sr.ReadToEnd();
                //responsefromserver = HttpUtility.UrlDecode(responsefromserver);
                XmlSerializer ser = new XmlSerializer(typeof(SecurePayResponse.SecurePayMessage));
                StringReader str = new StringReader(responsefromserver);
                SecurePayResponse.SecurePayMessage msg = (SecurePayResponse.SecurePayMessage)ser.Deserialize(str);
                if (msg.Status.statusCode == "000")
                {
                    result.NewPaymentStatus = (refundPaymentRequest.IsPartialRefund ? PaymentStatus.PartiallyRefunded : PaymentStatus.Refunded);
                }
                else
                {
                    result.AddError(msg.Status.statusCode + " " + msg.Status.statusDescription);
                }
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
            var sp = new SPMessage.SecurePayMessage
            {
                MerchantInfo = new SPMessage.MerchantInfo
                {
                    merchantID = _paypalStandardPaymentSettings.MerchantId,
                    password = _paypalStandardPaymentSettings.Password
                },
                RequestType = "Payment",
                MessageInfo = new SPMessage.MessageInfo
                {
                    apiVersion = "xml-4.2",
                    timeoutValue = "60",
                    messageID = voidPaymentRequest.Order.OrderGuid + DateTime.UtcNow.Hour.ToString() + DateTime.UtcNow.Minute.ToString() + DateTime.UtcNow.Second.ToString() + DateTime.UtcNow.Millisecond.ToString()
                },
                Payment = new SPMessage.Payment
                {
                    TxnList = new SPMessage.TxnList
                    {
                        count = "1",
                        Txn = new SPMessage.Txn
                        {
                            amount = ((int)(voidPaymentRequest.Order.OrderTotal * 100)).ToString(),
                            ID = "1",
                            currency = "AUD",
                            purchaseOrderNo = voidPaymentRequest.Order.OrderGuid.ToString(),
                            txnSource = "23",
                            txnType = "6",
                            txnID = voidPaymentRequest.Order.CaptureTransactionId
                        }
                    }
                }
            };

            XmlSerializer serializer = new XmlSerializer(typeof(SPMessage.SecurePayMessage));
            StringWriter stringWriter = new StringWriter();
            serializer.Serialize(stringWriter, sp);
            string message = stringWriter.ToString();
            //message = HttpUtility.UrlEncode(message);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(GetSecurePayAPIUrl(false, _paypalStandardPaymentSettings.TestAccount));
            request.ContentType = "text/xml";
            request.Method = "POST";
            StreamWriter sw = new StreamWriter(request.GetRequestStream());
            sw.Write(message);
            sw.Close();
            WebResponse response = request.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream());
            string responsefromserver = sr.ReadToEnd();
            //responsefromserver = HttpUtility.UrlDecode(responsefromserver);
            XmlSerializer ser = new XmlSerializer(typeof(SecurePayResponse.SecurePayMessage));
            StringReader str = new StringReader(responsefromserver);
            SecurePayResponse.SecurePayMessage msg = (SecurePayResponse.SecurePayMessage)ser.Deserialize(str);
            if (msg.Status.statusCode == "000")
            {
                result.NewPaymentStatus = PaymentStatus.Voided;
            }
            else
            {
                result.AddError(msg.Status.statusCode + " " + msg.Status.statusDescription);
            }
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
                throw new ArgumentNullException("order");

            return false;
        }

        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            return false;
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentSecurePayAPI/Configure";
        }

        public override void Install()
        {
            //settings
            var settings = new SecurePayAPIPaymentSettings()
            {
                Password = "",MerchantId="",TestAccount=true,UsePreauth=false,FraudGuard=true
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SecurePayAPI.Fields.Password", "Merchant Account Password");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SecurePayAPI.Fields.TestAccount", "Use the test account");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SecurePayAPI.Fields.MerchantId", "Merchant Id");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SecurePayAPI.Fields.FraudGuard", "Enable FraudGuard");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SecurePayAPI.Fields.UsePreauth", "Use preauth and capture");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SecurePayAPI.Fields.CardsAllowed", "Credit/Debit Cards Allowed");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SecurePayAPI.Fields.CVV", "CVV");
            base.Install();
        }
        
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<SecurePayAPIPaymentSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.SecurePayAPI.Fields.Password");
            this.DeletePluginLocaleResource("Plugins.Payments.SecurePayAPI.Fields.TestAccount");
            this.DeletePluginLocaleResource("Plugins.Payments.SecurePayAPI.Fields.MerchantId");
            this.DeletePluginLocaleResource("Plugins.Payments.SecurePayAPI.Fields.FraudGuard");
            this.DeletePluginLocaleResource("Plugins.Payments.SecurePayAPI.Fields.UsePreauth");
            this.DeletePluginLocaleResource("Plugins.Payments.SecurePayAPI.Fields.CardsAllowed");
            this.DeletePluginLocaleResource("Plugins.Payments.SecurePayAPI.Fields.CVV");
            base.Uninstall();
        }

        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel
            {
                CardholderName = form["CardholderName"],
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpireMonth = form["ExpireMonth"],
                ExpireYear = form["ExpireYear"]
            };
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                warnings.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));

            return warnings;
        }

        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            return new ProcessPaymentRequest
            {
                CreditCardType = form["CreditCardType"],
                CreditCardName = form["CardholderName"],
                CreditCardNumber = form["CardNumber"],
                CreditCardExpireMonth = int.Parse(form["ExpireMonth"]),
                CreditCardExpireYear = int.Parse(form["ExpireYear"]),
                CreditCardCvv2 = form["CardCode"]
            };
        }

        public void GetPublicViewComponent(out string viewComponentName)
        {
            viewComponentName = "PaymentSecurePay";
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture => true;

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund => true;

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund => true;

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid => true;

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;

        public bool SkipPaymentInfo => false;

        public string PaymentMethodDescription => "Credit/Debit Card";
        #endregion


    }
}
