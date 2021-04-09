using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Shipping.AustraliaPost.Models
{
    public record AustraliaPostShippingModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Shipping.AustraliaPost.Fields.ApiKey")]
        public string ApiKey { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.AustraliaPost.Fields.AdditionalHandlingCharge")]
        public decimal AdditionalHandlingCharge { get; set; }
    }
}