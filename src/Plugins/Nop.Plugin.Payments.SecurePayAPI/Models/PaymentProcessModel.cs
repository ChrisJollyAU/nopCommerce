using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.SecurePayAPI.Models
{
    public class PaymentProcessModel : BaseNopModel
    {
        public string MerchantId { get; set; }
        public string Hash { get; set; }
        public string TimeStamp { get; set; }
        public string PurchaseValue { get; set; }
        public bool IsTestAccount { get; set; }
        public int TxnType { get; set; }
        public string RefID { get; set; }
        public string ResultUrl { get; set; }

        public string PostAccount {
            get
            {
                if (IsTestAccount)
                {
                    return "https://api.securepay.com.au/test/directpost/authorise";
                }
                else
                {
                    return "https://api.securepay.com.au/live/directpost/authorise";
                }
            }
        }
    }
}