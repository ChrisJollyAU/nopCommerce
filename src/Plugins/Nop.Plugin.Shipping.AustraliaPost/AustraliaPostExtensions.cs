using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Nop.Core.Domain.Shipping;
using Nop.Services.Directory;

namespace Nop.Plugin.Shipping.AustraliaPost
{
    public static class AustraliaPostExtensions
    {
        public static async Task<ShippingOption> ParseShippingOptionAsync(this JObject obj, ICurrencyService currencyService)
        {
            var audCurrency = await currencyService.GetCurrencyByCodeAsync("AUD");
            if (obj.HasValues)
            {
                var shippingOption = new ShippingOption();
                foreach (var property in obj.Properties())
                {
                    switch (property.Name.ToLower())
                    {
                        case "name":
                            shippingOption.Name = $"Australia Post. {property.Value}";
                            break;
                        case "price":
                            if (decimal.TryParse(property.Value.ToString(), out decimal rate))
                            {
                                var convertedRate = await currencyService.ConvertToPrimaryStoreCurrencyAsync(rate, audCurrency);
                                shippingOption.Rate = convertedRate;
                            }
                            break;
                    }
                }
                return shippingOption;
            }

            return null;
        }
    }
}