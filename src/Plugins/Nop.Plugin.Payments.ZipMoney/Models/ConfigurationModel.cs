using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Mvc.Models;

namespace Nop.Plugin.Payments.ZipMoney.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.ZipMoney.Fields.UseSandBox")]
        public bool UseSandbox { get; set; }

        [NopResourceDisplayName("Plugins.Payments.ZipMoney.Fields.SandboxAPIKey")]
        public string SandboxAPIKey { get; set; }

        [NopResourceDisplayName("Plugins.Payments.ZipMoney.Fields.SandboxPublicKey")]
        public string SandboxPublicKey { get; set; }

        [NopResourceDisplayName("Plugins.Payments.ZipMoney.Fields.ProductionAPIKey")]
        public string ProductionAPIKey { get; set; }

        [NopResourceDisplayName("Plugins.Payments.ZipMoney.Fields.ProductionPublicKey")]
        public string ProductionPublicKey { get; set; }

        public bool UseSandbox_OverrideForStore { get; set; }
        public bool SandboxAPIKey_OverrideForStore { get; set; }

        public bool SandboxPublicKey_OverrideForStore { get; set; }

        public bool ProductionAPIKey_OverrideForStore { get; set; }

        public bool ProductionPublicKey_OverrideForStore { get; set; }
    }
}