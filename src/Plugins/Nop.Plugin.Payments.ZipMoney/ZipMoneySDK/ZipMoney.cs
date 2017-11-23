using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Plugin.Payments.ZipMoney.ZipMoneySDK.Models;

namespace Nop.Plugin.Payments.ZipMoney.ZipMoneySDK
{
    public class ZipMoney
    {
        private readonly bool _useSandbox;
        private HttpClient client;
        private readonly string _apiKey;
        public ZipMoney(bool useSandbox = false,string ApiKey)
        {
            _useSandbox = useSandbox;
            _apiKey = ApiKey;
            client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            client.DefaultRequestHeaders.Add("Content-Type", "application/json");
            client.DefaultRequestHeaders.Add("Zip-Version", "2017-03-01");
            client.DefaultRequestHeaders.Add("Idempotency-Key", "");
        }

        public async Task<ZipCheckoutResponse> CreateCheckout(Shopper shopper)
        {
            string shopser = JsonConvert.SerializeObject(shopper);
            string uri;
            if (_useSandbox) uri = "https://api.sandbox.zipmoney.com.au/merchant/v1/checkouts";
            else uri = "";
            var result = await client.PostAsync(uri,
                new StringContent(shopser, Encoding.UTF8, "application/json"));
            return JsonConvert.DeserializeObject<ZipCheckoutResponse>(await result.Content.ReadAsStringAsync());
        }

        public void CreateCharge()
        {
            
        }

        public void CaptureCharge(string chargeId, decimal amount)
        {
            
        }
    }
}
