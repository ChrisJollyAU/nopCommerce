using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.SecurePayAPI
{
    public class SecurePayAPIPaymentSettings : ISettings
    {
        public string Password { get; set; }

        public bool TestAccount { get; set; }

        public string MerchantId { get; set; }

        public bool FraudGuard { get; set; }

        public bool UsePreauth { get; set; }
    }
}
