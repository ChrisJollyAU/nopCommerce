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
        private readonly IPermissionService _permissionService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;

        public PaymentSecurePayAPIController(ISettingService settingService, 
            IPaymentService paymentService, IOrderService orderService, 
            IOrderProcessingService orderProcessingService, 
            IWebHelper webHelper, IPermissionService permissionService,
            SecurePayAPIPaymentSettings paypalStandardPaymentSettings,
            IWorkContext workContext, IStoreContext storeContext,
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
            _permissionService = permissionService;
            _workContext = workContext;
            _storeContext = storeContext;
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

            var model = new ConfigurationModel
            {
                Password = _paypalStandardPaymentSettings.Password,
                TestAccount = _paypalStandardPaymentSettings.TestAccount,
                MerchantId = _paypalStandardPaymentSettings.MerchantId,
                FraudGuard = _paypalStandardPaymentSettings.FraudGuard
            };
            return View("~/Plugins/Payments.SecurePayAPI/Views/Configure.cshtml", model);
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

        public IActionResult CancelOrder()
        {
            var order = _orderService.SearchOrders(storeId: _storeContext.CurrentStore.Id,
                customerId: _workContext.CurrentCustomer.Id, pageSize: 1).FirstOrDefault();
            if (order != null)
                return RedirectToRoute("OrderDetails", new { orderId = order.Id });

            return RedirectToRoute("HomePage");
        }
    }
}