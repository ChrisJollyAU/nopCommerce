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
        private ZipErrorContainer error;
        private string _partnerTag;
        public ZipMoneyProcessor(string ApiKey, bool useSandbox = false,string partnerTag= "GeneralPartner")
        {
            _useSandbox = useSandbox;
            _apiKey = ApiKey;
            client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            //client.DefaultRequestHeaders.Add("Content-Type", "application/json");
            client.DefaultRequestHeaders.Add("Zip-Version", "2017-03-01");
            client.DefaultRequestHeaders.Add("Idempotency-Key", "");
            _partnerTag = partnerTag;
        }

        public ZipErrorContainer GetLastError()
        {
            return error;
        }

        public async Task<ZipCheckoutResponse> CreateCheckout(ZipCheckoutRequest checkout)
        {
            error = null;
            string checkoutser = JsonConvert.SerializeObject(checkout);
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/checkouts" : "https://api.zipmoney.com.au/merchant/v1/checkouts/";
            if (!checkout.metadata.ContainsKey("partner"))
                checkout.metadata["partner"] = _partnerTag;
            var result = await client.PostAsync(uri,
                new StringContent(checkoutser, Encoding.UTF8, "application/json"));
            if (result.IsSuccessStatusCode)
            {
                var result2 = 
                    JsonConvert.DeserializeObject<ZipCheckoutResponse>(await result.Content.ReadAsStringAsync());
                result2.redirect_uri = result2.uri;
                return result2;
            }
            error = JsonConvert.DeserializeObject<ZipErrorContainer>(await result.Content.ReadAsStringAsync());
            //a blank/empty response
            //allows the calling code to pass the object straight back to lightbox
            //as the uri/redirect_uri is empty the lightbox will close automatically
            return new ZipCheckoutResponse();
        }

        public async Task<ZipCheckoutRequest> RetreiveCheckout(string checkoutId)
        {
            error = null;
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/checkouts/" : "https://api.zipmoney.com.au/merchant/v1/checkouts/";
            uri += checkoutId;
            var response = await client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ZipCheckoutRequest>(await response.Content.ReadAsStringAsync());
            }
            error = JsonConvert.DeserializeObject<ZipErrorContainer>(await response.Content.ReadAsStringAsync());
            return new ZipCheckoutRequest();
        }

        public async Task<ZipChargeResponse> CreateCharge(ZipChargeRequest zipCharge)
        {
            error = null;
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/charges/" : "https://api.zipmoney.com.au/merchant/v1/charges/";
            var result = await client.PostAsync(uri, new StringContent(JsonConvert.SerializeObject(zipCharge),Encoding.UTF8,"application/json"));
            if (result.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ZipChargeResponse>(await result.Content.ReadAsStringAsync());
            }
            error = JsonConvert.DeserializeObject<ZipErrorContainer>(await result.Content.ReadAsStringAsync());
            return new ZipChargeResponse();
        }

        public async Task<ZipChargeResponse> CaptureCharge(string chargeId, decimal amount)
        {
            error = null;
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/charges/" : "https://api.zipmoney.com.au/merchant/v1/charges/";
            uri += chargeId + "/capture";
            string content = "{\"amount\": " + amount + "}";
            var result = await client.PostAsync(uri, new StringContent(content,Encoding.UTF8,"application/json"));
            if (result.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ZipChargeResponse>(await result.Content.ReadAsStringAsync());
            }
            error = JsonConvert.DeserializeObject<ZipErrorContainer>(await result.Content.ReadAsStringAsync());
            return new ZipChargeResponse();
        }

        public async Task<ZipChargeResponse> CancelCharge(string chargeId)
        {
            error = null;
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/charges/" : "https://api.zipmoney.com.au/merchant/v1/charges/";
            uri += chargeId + "/cancel";
            var result = await client.PostAsync(uri, new StringContent("{}", Encoding.UTF8, "application/json"));
            if (result.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ZipChargeResponse>(await result.Content.ReadAsStringAsync());
            }
            error = JsonConvert.DeserializeObject<ZipErrorContainer>(await result.Content.ReadAsStringAsync());
            return new ZipChargeResponse();
        }

        public async Task<ZipChargeResponse> RetrieveCharge(string chargeId)
        {
            error = null;
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/charges/" : "https://api.zipmoney.com.au/merchant/v1/charges/";
            uri += chargeId;
            var result = await client.GetAsync(uri);
            if (result.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ZipChargeResponse>(await result.Content.ReadAsStringAsync());
            }
            error = JsonConvert.DeserializeObject<ZipErrorContainer>(await result.Content.ReadAsStringAsync());
            return new ZipChargeResponse();
        }

        public async Task<ZipChargeCollection> ListCharges(int skip = 0, int limit = 0, string state = "",
            string expand = "")
        {
            return new ZipChargeCollection();
        }

        public async Task<ZipRefundResponse> CreateRefund(string chargeId, string reason, decimal amount)
        {
            error = null;
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/refunds/" : "https://api.zipmoney.com.au/merchant/v1/refunds/";
            Dictionary<string, string> vals = new Dictionary<string, string>
            {
                ["charged_id"] = chargeId,
                ["reason"] = reason,
                ["amount"] = amount.ToString(CultureInfo.InvariantCulture)
            };
            var response = await client.PostAsync(uri, new StringContent(JsonConvert.SerializeObject(vals),Encoding.UTF8,"application/json"));
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ZipRefundResponse>(await response.Content.ReadAsStringAsync());
            }
            error = JsonConvert.DeserializeObject<ZipErrorContainer>(await response.Content.ReadAsStringAsync());
            return new ZipRefundResponse();
        }

        public async Task<ZipRefundResponse> RetreiveRefund(string refundId)
        {
            error = null;
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/refunds/" : "https://api.zipmoney.com.au/merchant/v1/refunds/";
            uri += refundId;
            var result = await client.GetAsync(uri);
            if (result.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ZipRefundResponse>(await result.Content.ReadAsStringAsync());
            }
            error = JsonConvert.DeserializeObject<ZipErrorContainer>(await result.Content.ReadAsStringAsync());
            return new ZipRefundResponse();
        }

        public async Task<ZipRefundCollection> ListRefunds(string chargeId,int skip,int limit)
        {
            return new ZipRefundCollection();
        }

        public async Task<ZipTokenResponse> CreateToken(string checkoutId)
        {
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/tokens/" : "https://api.zipmoney.com.au/merchant/v1/tokens/";
            ZipTokenRequest tokenRequest = new ZipTokenRequest
            {
                authority = new ZipAuthority
                {
                    type = AuthorityType.checkout_id,
                    value = checkoutId
                }
            };
            var response = await client.PostAsync(uri, new StringContent(JsonConvert.SerializeObject(tokenRequest),Encoding.UTF8,"application/json"));
            return JsonConvert.DeserializeObject<ZipTokenResponse>(await response.Content.ReadAsStringAsync());
        }
    }
}
