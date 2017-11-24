using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ZipMoneySDK.Models;

namespace ZipMoneySDK
{
    public class ZipMoneyProcessor
    {
        private readonly bool _useSandbox;
        private HttpClient client;
        private readonly string _apiKey;
        public ZipMoneyProcessor(bool useSandbox = false,string ApiKey)
        {
            _useSandbox = useSandbox;
            _apiKey = ApiKey;
            client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            client.DefaultRequestHeaders.Add("Content-Type", "application/json");
            client.DefaultRequestHeaders.Add("Zip-Version", "2017-03-01");
            client.DefaultRequestHeaders.Add("Idempotency-Key", "");
        }

        public async Task<ZipCheckoutResponse> CreateCheckout(ZipCheckout checkout)
        {
            string checkoutser = JsonConvert.SerializeObject(checkout);
            string uri;
            if (_useSandbox) uri = "https://api.sandbox.zipmoney.com.au/merchant/v1/checkouts";
            else uri = "";
            var result = await client.PostAsync(uri,
                new StringContent(checkoutser, Encoding.UTF8, "application/json"));
            return JsonConvert.DeserializeObject<ZipCheckoutResponse>(await result.Content.ReadAsStringAsync());
        }

        public int CreateCharge(ZipCharge zipCharge)
        {
            
        }

        public void CaptureCharge(string chargeId, decimal amount)
        {
            
        }
    }
}
