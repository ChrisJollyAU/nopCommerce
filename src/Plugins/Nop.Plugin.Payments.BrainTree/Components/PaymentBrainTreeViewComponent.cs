using System;
using System.Linq;
using Braintree;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Plugin.Payments.BrainTree.Models;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.BrainTree.Components
{
    [ViewComponent(Name = "PaymentBrainTree")]
    public class PaymentBrainTreeViewComponent : NopViewComponent
    {
        private readonly BrainTreePaymentSettings _brainTreePaymentSettings;

        public PaymentBrainTreeViewComponent(BrainTreePaymentSettings brainTreePaymentSettings)
        {
            this._brainTreePaymentSettings = brainTreePaymentSettings;
        }
        public IViewComponentResult Invoke()
        {
            var model = new PaymentInfoModel();
            var useSandBox = _brainTreePaymentSettings.UseSandBox;
            var merchantId = _brainTreePaymentSettings.MerchantId;
            var publicKey = _brainTreePaymentSettings.PublicKey;
            var privateKey = _brainTreePaymentSettings.PrivateKey;

            //new gateway
            var gateway = new BraintreeGateway
            {
                Environment = useSandBox ? Braintree.Environment.SANDBOX : Braintree.Environment.PRODUCTION,
                MerchantId = merchantId,
                PublicKey = publicKey,
                PrivateKey = privateKey
            };

            var clientToken = gateway.ClientToken.Generate();
            model.Token = clientToken;
            return View("~/Plugins/Payments.BrainTree/Views/PaymentInfo.cshtml", model);
        }
    }
}