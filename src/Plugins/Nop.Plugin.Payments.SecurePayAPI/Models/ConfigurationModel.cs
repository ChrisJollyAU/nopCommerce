using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Mvc.Models;

namespace Nop.Plugin.Payments.SecurePayAPI.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payments.SecurePayAPI.Fields.Password")]
        public string Password { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SecurePayAPI.Fields.TestAccount")]
        public bool TestAccount { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SecurePayAPI.Fields.MerchantId")]
        public string MerchantId { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SecurePayAPI.Fields.FraudGuard")]
        public bool FraudGuard { get; set; }
    }
}