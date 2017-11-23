using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Nop.Plugin.Payments.ZipMoney.Models;
using Nop.Services.Configuration;
using Nop.Services.Payments;
using Nop.Services.Orders;
using Nop.Core;
using Nop.Core.Domain.Payments;
using Nop.Services.Localization;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Nop.Plugin.Payments.ZipMoney.ZipMoneySDK.Models;
using Nop.Web.Framework;
using Nop.Services.Security;
using Nop.Services.Stores;

namespace Nop.Plugin.Payments.ZipMoney.Controllers
{
    public class PaymentZipMoneyController : BasePaymentController
    {
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IWebHelper _webHelper;
        private readonly ZipMoneyPaymentSettings _zipMoneyPaymentSettings;
        private readonly PaymentSettings _paymentSettings;
        private readonly ILocalizationService _localizationService;
        private readonly IPermissionService _permissionService;
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly IStoreContext _storeContext;

        public PaymentZipMoneyController(ISettingService settingService,
            IWorkContext workContext,
            IStoreService storeService,
            IPaymentService paymentService, IOrderService orderService, 
            IOrderProcessingService orderProcessingService, 
            IWebHelper webHelper,
            ZipMoneyPaymentSettings zipMoneyPaymentSettings,
            IStoreContext storeContext,
            PaymentSettings paymentSettings,
            IPermissionService permissionService,
            ILocalizationService localizationService)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._webHelper = webHelper;
            this._zipMoneyPaymentSettings = zipMoneyPaymentSettings;
            this._paymentSettings = paymentSettings;
            this._localizationService = localizationService;
            this._permissionService = permissionService;
            this._storeContext = storeContext;
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            //whether user has the authority
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var zipMoneyPaymentSettings = _settingService.LoadSetting<ZipMoneyPaymentSettings>(storeScope);

            var model = new ConfigurationModel
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
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(zipMoneyPaymentSettings, x => x.UseSandbox, storeScope);
                model.SandboxAPIKey_OverrideForStore = _settingService.SettingExists(zipMoneyPaymentSettings, x => x.SandboxAPIKey, storeScope);
                model.SandboxPublicKey_OverrideForStore = _settingService.SettingExists(zipMoneyPaymentSettings, x => x.SandboxPublicKey, storeScope);
                model.ProductionAPIKey_OverrideForStore = _settingService.SettingExists(zipMoneyPaymentSettings, x => x.ProductionAPIKey, storeScope);
                model.ProductionPublicKey_OverrideForStore = _settingService.SettingExists(zipMoneyPaymentSettings, x => x.ProductionPublicKey, storeScope);
            }

            return View("~/Plugins/Payments.ZipMoney/Views/Configure.cshtml", model);
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

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var zipMoneyPaymentSettings = _settingService.LoadSetting<ZipMoneyPaymentSettings>(storeScope);

            //save settings
            zipMoneyPaymentSettings.UseSandbox = model.UseSandbox;
            zipMoneyPaymentSettings.SandboxAPIKey = model.SandboxAPIKey;
            zipMoneyPaymentSettings.SandboxPublicKey = model.SandboxPublicKey;
            zipMoneyPaymentSettings.ProductionAPIKey = model.ProductionAPIKey;
            zipMoneyPaymentSettings.ProductionPublicKey = model.ProductionPublicKey;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(zipMoneyPaymentSettings, x => x.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(zipMoneyPaymentSettings, x => x.SandboxAPIKey, model.SandboxAPIKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(zipMoneyPaymentSettings, x => x.SandboxPublicKey, model.SandboxPublicKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(zipMoneyPaymentSettings, x => x.ProductionAPIKey, model.ProductionAPIKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(zipMoneyPaymentSettings, x => x.ProductionPublicKey, model.ProductionPublicKey_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

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

        public async Task<string> ZipCheckout()
        {
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var zipMoneyPaymentSettings = _settingService.LoadSetting<ZipMoneyPaymentSettings>(storeScope);
            HttpClient client = new HttpClient();
            Shopper shopper = new Shopper();
            string shopser = JsonConvert.SerializeObject(shopper);
            string apikey = zipMoneyPaymentSettings.UseSandbox
                ? zipMoneyPaymentSettings.SandboxAPIKey
                : zipMoneyPaymentSettings.ProductionAPIKey;
            ZipMoney.ZipMoneySDK.ZipMoney zm = new ZipMoneySDK.ZipMoney(true,apikey);
            ZipCheckoutResponse zcr = await zm.CreateCheckout(shopper);
            return JsonConvert.SerializeObject(zcr);
        }

        public IActionResult ZipRedirect()
        {
            
        }
    }
}