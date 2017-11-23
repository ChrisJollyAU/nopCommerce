using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.ZipMoney
{
    public class ZipMoneyPaymentSettings : ISettings
    {
        public string SandboxAPIKey { get; set; }

        public string SandboxPublicKey { get; set; }

        public bool UseSandbox { get; set; }

        public string ProductionAPIKey { get; set; }

        public string ProductionPublicKey { get; set; }
    }
}
