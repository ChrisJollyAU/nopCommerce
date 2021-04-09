using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.BrainTree.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.BrainTree.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    public class PaymentBrainTreeController : BasePaymentController
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly IPermissionService _permissionService;
        private readonly IStoreContext _storeContext;
        private readonly INotificationService _notificationService;
        #endregion

        #region Ctor

        public PaymentBrainTreeController(ISettingService settingService,
            ILocalizationService localizationService, 
            IWorkContext workContext, 
            IStoreService storeService,
            IStoreContext storeContext,
            INotificationService notificationService,
            IPermissionService permissionService)
        {
            this._settingService = settingService;
            this._localizationService = localizationService;
            this._workContext = workContext;
            this._storeService = storeService;
            this._permissionService = permissionService;
            _storeContext = storeContext;
            _notificationService = notificationService;
        }

        #endregion

        #region Methods

        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var brainTreePaymentSettings = await _settingService.LoadSettingAsync<BrainTreePaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                ActiveStoreScopeConfiguration = storeScope,
                UseSandBox = brainTreePaymentSettings.UseSandBox,
                PublicKey = brainTreePaymentSettings.PublicKey,
                PrivateKey = brainTreePaymentSettings.PrivateKey,
                MerchantId = brainTreePaymentSettings.MerchantId,
                AdditionalFee = brainTreePaymentSettings.AdditionalFee,
                AdditionalFeePercentage = brainTreePaymentSettings.AdditionalFeePercentage
            };

            if (storeScope > 0)
            {
                model.UseSandbox_OverrideForStore = await _settingService.SettingExistsAsync(brainTreePaymentSettings, x => x.UseSandBox, storeScope);
                model.PublicKey_OverrideForStore = await _settingService.SettingExistsAsync(brainTreePaymentSettings, x => x.PublicKey, storeScope);
                model.PrivateKey_OverrideForStore = await _settingService.SettingExistsAsync(brainTreePaymentSettings, x => x.PrivateKey, storeScope);
                model.MerchantId_OverrideForStore = await _settingService.SettingExistsAsync(brainTreePaymentSettings, x => x.MerchantId, storeScope);
                model.AdditionalFee_OverrideForStore = await _settingService.SettingExistsAsync(brainTreePaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = await _settingService.SettingExistsAsync(brainTreePaymentSettings, x => x.AdditionalFeePercentage, storeScope);
            }

            return View("~/Plugins/Payments.BrainTree/Views/Configure.cshtml", model);
        }

        [HttpPost]
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var brainTreePaymentSettings = await _settingService.LoadSettingAsync<BrainTreePaymentSettings>(storeScope);

            //save settings
            brainTreePaymentSettings.UseSandBox = model.UseSandBox;
            brainTreePaymentSettings.PublicKey = model.PublicKey;
            brainTreePaymentSettings.PrivateKey = model.PrivateKey;
            brainTreePaymentSettings.MerchantId = model.MerchantId;
            brainTreePaymentSettings.AdditionalFee = model.AdditionalFee;
            brainTreePaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            await _settingService.SaveSettingOverridablePerStoreAsync(brainTreePaymentSettings, x => x.UseSandBox, model.UseSandbox_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(brainTreePaymentSettings, x => x.PublicKey, model.PublicKey_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(brainTreePaymentSettings, x => x.PrivateKey, model.PrivateKey_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(brainTreePaymentSettings, x => x.MerchantId, model.MerchantId_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(brainTreePaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(brainTreePaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        #endregion
    }
}