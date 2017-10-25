using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nop.Plugin.Payments.SecurePayAPI.Models;
using Nop.Plugin.Payments.SecurePayAPI.Validators;
using Nop.Services.Configuration;
using Nop.Services.Payments;
using Nop.Services.Orders;
using Nop.Core;
using Nop.Core.Domain.Payments;
using Nop.Services.Localization;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Services.Security;

namespace Nop.Plugin.Payments.SecurePayAPI.Controllers
{
    public class PaymentSecurePayAPIController : BasePaymentController
    {
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IWebHelper _webHelper;
        private readonly SecurePayAPIPaymentSettings _paypalStandardPaymentSettings;
        private readonly PaymentSettings _paymentSettings;
        private readonly ILocalizationService _localizationService;

        public PaymentSecurePayAPIController(ISettingService settingService, 
            IPaymentService paymentService, IOrderService orderService, 
            IOrderProcessingService orderProcessingService, 
            IWebHelper webHelper,
            SecurePayAPIPaymentSettings paypalStandardPaymentSettings,
            PaymentSettings paymentSettings, ILocalizationService localizationService)
        {
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._webHelper = webHelper;
            this._paypalStandardPaymentSettings = paypalStandardPaymentSettings;
            this._paymentSettings = paymentSettings;
            this._localizationService = localizationService;
        }

        private string CreateSHA1Signature(string RawData)
        {
            /*
             <summary>Creates a MD5 Signature</summary>
             <param name="RawData">The string used to create the MD5 signautre.</param>
             <returns>A string containing the MD5 signature.</returns>
             */

            System.Security.Cryptography.SHA1 hasher = System.Security.Cryptography.SHA1CryptoServiceProvider.Create();
            byte[] HashValue = hasher.ComputeHash(Encoding.ASCII.GetBytes(RawData));

            string strHex = "";
            foreach (byte b in HashValue)
            {
                strHex += b.ToString("x2");
            }
            return strHex.ToUpper();
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            //whether user has the authority
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            var model = new ConfigurationModel();
            model.Password = _paypalStandardPaymentSettings.Password;
            model.TestAccount = _paypalStandardPaymentSettings.TestAccount;
            model.MerchantId = _paypalStandardPaymentSettings.MerchantId;
            model.FraudGuard = _paypalStandardPaymentSettings.FraudGuard;
            return View("~/Plugins/Payments.SecurePayAPI/Views/PaymentSecurePayAPI/Configure.cshtml", model);
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

            //save settings
            _paypalStandardPaymentSettings.Password = model.Password;
            _paypalStandardPaymentSettings.TestAccount = model.TestAccount;
            _paypalStandardPaymentSettings.MerchantId = model.MerchantId;
            _paypalStandardPaymentSettings.FraudGuard = model.FraudGuard;
            _settingService.SaveSetting(_paypalStandardPaymentSettings);
            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));
            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            var model = new PaymentInfoModel();

            //years
            for (int i = 0; i < 15; i++)
            {
                string year = Convert.ToString(DateTime.Now.Year + i);
                model.ExpireYears.Add(new SelectListItem()
                {
                    Text = year,
                    Value = year,
                });
            }

            //months
            for (int i = 1; i <= 12; i++)
            {
                string text = (i < 10) ? "0" + i.ToString() : i.ToString();
                model.ExpireMonths.Add(new SelectListItem()
                {
                    Text = text,
                    Value = i.ToString(),
                });
            }

            //set postback values
            var form = this.Request.Form;
            model.CardNumber = form["CardNumber"];
            model.CardCode = form["CardCode"];
            var selectedMonth = model.ExpireMonths.Where(x => x.Value.Equals(form["ExpireMonth"], StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (selectedMonth != null)
                selectedMonth.Selected = true;
            var selectedYear = model.ExpireYears.Where(x => x.Value.Equals(form["ExpireYear"], StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (selectedYear != null)
                selectedYear.Selected = true;

            return View("~/Plugins/Payments.SecurePayAPI/Views/PaymentSecurePayAPI/PaymentInfo.cshtml", model);
        }

        [ValidateInput(false)]
        public ActionResult AcceptPayment()
        {
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.SecurePay") as SecurePayAPIPaymentProcessor;
            if (processor == null ||
                !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("SecurePay module cannot be loaded");

            return null;
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel()
            {
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpireMonth = form["ExpireMonth"],
                ExpireYear = form["ExpireYear"]
            };
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                foreach (var error in validationResult.Errors)
                    warnings.Add(error.ErrorMessage);
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            paymentInfo.CreditCardNumber = form["CardNumber"];
            paymentInfo.CreditCardExpireMonth = int.Parse(form["ExpireMonth"]);
            paymentInfo.CreditCardExpireYear = int.Parse(form["ExpireYear"]);
            paymentInfo.CreditCardCvv2 = form["CardCode"];
            return paymentInfo;
        }

        public ActionResult CancelOrder(FormCollection form)
        {
            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}