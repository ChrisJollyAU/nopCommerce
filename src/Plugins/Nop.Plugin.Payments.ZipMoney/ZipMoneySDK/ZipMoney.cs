using System;
using System.Collections.Generic;
using System.Globalization;
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
        private ZipError error;
        public ZipMoneyProcessor(string ApiKey, bool useSandbox = false)
        {
            _useSandbox = useSandbox;
            _apiKey = ApiKey;
            client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            //client.DefaultRequestHeaders.Add("Content-Type", "application/json");
            client.DefaultRequestHeaders.Add("Zip-Version", "2017-03-01");
            client.DefaultRequestHeaders.Add("Idempotency-Key", "");
        }

        public async Task<ZipCheckoutResponse> CreateCheckout(ZipCheckout checkout)
        {
            error = null;
            string checkoutser = JsonConvert.SerializeObject(checkout);
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/checkouts" : "";
            var result = await client.PostAsync(uri,
                new StringContent(checkoutser, Encoding.UTF8, "application/json"));
            if (result.IsSuccessStatusCode)
            {
                var result2 = 
                    JsonConvert.DeserializeObject<ZipCheckoutResponse>(await result.Content.ReadAsStringAsync());
                result2.redirect_uri = result2.uri;
                return result2;
            }
            error = JsonConvert.DeserializeObject<ZipError>(await result.Content.ReadAsStringAsync());
            //a blank/empty response
            //allows the calling code to pass the object straight back to lightbox
            //as the uri/redirect_uri is empty the lightbox will close automatically
            return new ZipCheckoutResponse();
        }

        public async Task<ZipCheckout> RetreiveCheckout(string checkoutId)
        {
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/checkouts/" : "";
            uri += checkoutId;
            string response = await client.GetStringAsync(uri);
            return JsonConvert.DeserializeObject<ZipCheckout>(response);
        }

        public async Task<ZipBaseResponse> CreateCharge(ZipCharge zipCharge)
        {
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/charges/" : "";
            var result = await client.PostAsync(uri, new StringContent(JsonConvert.SerializeObject(zipCharge),Encoding.UTF8,"application/json"));
            return JsonConvert.DeserializeObject<ZipBaseResponse>(await result.Content.ReadAsStringAsync());
        }

        public async Task<ZipBaseResponse> CaptureCharge(string chargeId, decimal amount)
        {
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/charges/" : "";
            uri += chargeId + "/capture";
            string content = "{\"amount\": " + amount + "}";
            var result = await client.PostAsync(uri, new StringContent(content,Encoding.UTF8,"application/json"));
            return JsonConvert.DeserializeObject<ZipBaseResponse>(await result.Content.ReadAsStringAsync());
        }

        public async Task<ZipBaseResponse> CancelCharge(string chargeId)
        {
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/charges/" : "";
            uri += chargeId + "/cancel";
            var result = await client.PostAsync(uri, new StringContent("{}", Encoding.UTF8, "application/json"));
            return JsonConvert.DeserializeObject<ZipBaseResponse>(await result.Content.ReadAsStringAsync());
        }

        public async Task<ZipBaseResponse> RetrieveCharge(string chargeId)
        {
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/charges/" : "";
            uri += chargeId;
            var result = await client.GetStringAsync(uri);
            return JsonConvert.DeserializeObject<ZipBaseResponse>(result);
        }

        public async Task<ZipBaseResponse> ListCharges(int skip = 0, int limit = 0, string state = "",
            string expand = "")
        {
            return null;
        }

        public async Task<ZipRefundResponse> CreateRefund(string chargeId, string reason, decimal amount)
        {
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/refunds" : "";
            Dictionary<string, string> vals = new Dictionary<string, string>
            {
                ["charged_id"] = chargeId,
                ["reason"] = reason,
                ["amount"] = amount.ToString(CultureInfo.InvariantCulture)
            };
            var response = await client.PostAsync(uri, new StringContent(JsonConvert.SerializeObject(vals),Encoding.UTF8,"application/json"));
            return JsonConvert.DeserializeObject<ZipRefundResponse>(await response.Content.ReadAsStringAsync());
        }

        public async Task<ZipRefundResponse> RetreiveRefund(string refundId)
        {
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/refunds/" : "";
            uri += refundId;
            var result = await client.GetStringAsync(uri);
            return JsonConvert.DeserializeObject<ZipRefundResponse>(result);
        }

        public async Task<ZipBaseResponse> ListRefunds(string chargeId,int skip,int limit)
        {
            return null;
        }

        public async Task<ZipTokenResponse> CreateToken(string checkoutId)
        {
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/tokens" : "";
            ZipAuthority authority = new ZipAuthority
            {
                type = "checkout_id",
                value = checkoutId
            };
            var response = await client.PostAsync(uri, new StringContent(JsonConvert.SerializeObject(authority),Encoding.UTF8,"application/json"));
            return JsonConvert.DeserializeObject<ZipTokenResponse>(await response.Content.ReadAsStringAsync());
        }
    }
}
