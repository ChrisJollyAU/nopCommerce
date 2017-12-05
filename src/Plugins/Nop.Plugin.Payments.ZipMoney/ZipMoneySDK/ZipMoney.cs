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
        private string lastresponse;
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

        public string GetLastResponse()
        {
            return lastresponse;
        }

        public async Task<ZipCheckoutResponse> CreateCheckout(ZipCheckoutRequest checkout)
        {
            error = null;
            lastresponse = "";
            string checkoutser = JsonConvert.SerializeObject(checkout);
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/checkouts" : "https://api.zipmoney.com.au/merchant/v1/checkouts/";
            if (checkout.metadata == null) checkout.metadata = new Dictionary<string, string>();
            if (!checkout.metadata.ContainsKey("partner"))
                checkout.metadata["partner"] = _partnerTag;
            var result = await client.PostAsync(uri,
                new StringContent(checkoutser, Encoding.UTF8, "application/json"));
            lastresponse = await result.Content.ReadAsStringAsync();
            if (result.IsSuccessStatusCode)
            {
                var result2 = 
                    JsonConvert.DeserializeObject<ZipCheckoutResponse>(lastresponse);
                result2.redirect_uri = result2.uri;
                return result2;
            }
            error = JsonConvert.DeserializeObject<ZipErrorContainer>(lastresponse);
            //a blank/empty response
            //allows the calling code to pass the object straight back to lightbox
            //as the uri/redirect_uri is empty the lightbox will close automatically
            return new ZipCheckoutResponse();
        }

        public async Task<ZipCheckoutRequest> RetreiveCheckout(string checkoutId)
        {
            error = null;
            lastresponse = "";
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/checkouts/" : "https://api.zipmoney.com.au/merchant/v1/checkouts/";
            uri += checkoutId;
            var response = await client.GetAsync(uri);
            lastresponse = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ZipCheckoutRequest>(lastresponse);
            }
            error = JsonConvert.DeserializeObject<ZipErrorContainer>(lastresponse);
            return new ZipCheckoutRequest();
        }

        public async Task<ZipChargeResponse> CreateCharge(ZipChargeRequest zipCharge)
        {
            error = null;
            lastresponse = "";
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/charges/" : "https://api.zipmoney.com.au/merchant/v1/charges/";
            var result = await client.PostAsync(uri, new StringContent(JsonConvert.SerializeObject(zipCharge),Encoding.UTF8,"application/json"));
            lastresponse = await result.Content.ReadAsStringAsync();
            if (result.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ZipChargeResponse>(lastresponse);
            }
            error = JsonConvert.DeserializeObject<ZipErrorContainer>(lastresponse);
            return new ZipChargeResponse();
        }

        public async Task<ZipChargeResponse> CaptureCharge(string chargeId, decimal amount)
        {
            error = null;
            lastresponse = "";
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/charges/" : "https://api.zipmoney.com.au/merchant/v1/charges/";
            uri += chargeId + "/capture";
            string content = "{\"amount\": " + amount + "}";
            var result = await client.PostAsync(uri, new StringContent(content,Encoding.UTF8,"application/json"));
            lastresponse = await result.Content.ReadAsStringAsync();
            if (result.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ZipChargeResponse>(lastresponse);
            }
            error = JsonConvert.DeserializeObject<ZipErrorContainer>(lastresponse);
            return new ZipChargeResponse();
        }

        public async Task<ZipChargeResponse> CancelCharge(string chargeId)
        {
            error = null;
            lastresponse = "";
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/charges/" : "https://api.zipmoney.com.au/merchant/v1/charges/";
            uri += chargeId + "/cancel";
            var result = await client.PostAsync(uri, new StringContent("{}", Encoding.UTF8, "application/json"));
            lastresponse = await result.Content.ReadAsStringAsync();
            if (result.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ZipChargeResponse>(lastresponse);
            }
            error = JsonConvert.DeserializeObject<ZipErrorContainer>(lastresponse);
            return new ZipChargeResponse();
        }

        public async Task<ZipChargeResponse> RetrieveCharge(string chargeId)
        {
            error = null;
            lastresponse = "";
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/charges/" : "https://api.zipmoney.com.au/merchant/v1/charges/";
            uri += chargeId;
            var result = await client.GetAsync(uri);
            lastresponse = await result.Content.ReadAsStringAsync();
            if (result.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ZipChargeResponse>(lastresponse);
            }
            error = JsonConvert.DeserializeObject<ZipErrorContainer>(lastresponse);
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
            lastresponse = "";
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/refunds/" : "https://api.zipmoney.com.au/merchant/v1/refunds/";
            Dictionary<string, string> vals = new Dictionary<string, string>
            {
                ["charged_id"] = chargeId,
                ["reason"] = reason,
                ["amount"] = amount.ToString(CultureInfo.InvariantCulture)
            };
            var response = await client.PostAsync(uri, new StringContent(JsonConvert.SerializeObject(vals),Encoding.UTF8,"application/json"));
            lastresponse = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ZipRefundResponse>(lastresponse);
            }
            error = JsonConvert.DeserializeObject<ZipErrorContainer>(lastresponse);
            return new ZipRefundResponse();
        }

        public async Task<ZipRefundResponse> RetreiveRefund(string refundId)
        {
            error = null;
            lastresponse = "";
            string uri = _useSandbox ? "https://api.sandbox.zipmoney.com.au/merchant/v1/refunds/" : "https://api.zipmoney.com.au/merchant/v1/refunds/";
            uri += refundId;
            var result = await client.GetAsync(uri);
            lastresponse = await result.Content.ReadAsStringAsync();
            if (result.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ZipRefundResponse>(lastresponse);
            }
            error = JsonConvert.DeserializeObject<ZipErrorContainer>(lastresponse);
            return new ZipRefundResponse();
        }

        public async Task<ZipRefundCollection> ListRefunds(string chargeId,int skip,int limit)
        {
            return new ZipRefundCollection();
        }

        public async Task<ZipTokenResponse> CreateToken(string checkoutId)
        {
            error = null;
            lastresponse = "";
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
            lastresponse = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ZipTokenResponse>(lastresponse);
            }
            error = JsonConvert.DeserializeObject<ZipErrorContainer>(lastresponse);
            return new ZipTokenResponse();
        }
    }
}
