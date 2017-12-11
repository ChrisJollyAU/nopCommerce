using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Mvc.Models;

namespace Nop.Plugin.Shipping.Temando.Models
{
    public class TemandoShippingModel : BaseNopModel
    {
        [NopResourceDisplayName("Username")]
        public string Username { get; set; }

        [NopResourceDisplayName("Password")]
        public string Password { get; set; }

        [NopResourceDisplayName("Free shipping over this threshold")]
        public decimal Threshold { get; set; }
    }
}