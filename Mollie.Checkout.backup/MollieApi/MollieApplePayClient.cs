using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using EPiServer.ServiceLocation;
using Mollie.Api.Client;
using Mollie.Checkout.Helpers;
using Newtonsoft.Json;

namespace Mollie.Checkout.MollieApi
{
    public class MollieApplePayClient : BaseMollieClient
    {
        private readonly ServiceAccessor<HttpContextBase> _httpContextAccessor;

        public MollieApplePayClient(string apiKey, HttpClient httpClient = null)
            : base(apiKey, httpClient)
        {

            _httpContextAccessor = ServiceLocator.Current.GetInstance<ServiceAccessor<HttpContextBase>>();
        }

        public ReponseValidateMerchant ValidateMerchant(string validationUrl)
        {
            var request = _httpContextAccessor().Request;

            var data = new Dictionary<string, string>
            {
                { "validationUrl", validationUrl },
                { "domain", request.Url?.Authority }
            };
            
            var response = AsyncHelper.RunSync(() =>
                this.PostAsync<ReponseValidateMerchant>("wallets/applepay/sessions", data));

            return response;
        }

        public void CreatePayment(object applePayPayment)
        {
            //var data = new Dictionary<string, string>
            //{
            //    { "method", "applepay" },
            //    { "domain", "923a-92-64-221-225.ngrok.io" }
            //};

            //var response = AsyncHelper.RunSync(() =>
            //    this.PostAsync<ReponseValidateMerchant>("wallets/applepay/sessions", data));
        }
    }

    public class ReponseValidateMerchant
    {
        [JsonProperty("epochTimestamp")]
        public string epochTimestamp { get; set; }

        [JsonProperty("expiresAt")]
        public string expiresAt { get; set; }

        [JsonProperty("merchantSessionIdentifier")]
        public string merchantSessionIdentifier { get; set; }

        [JsonProperty("nonce")]
        public string nonce { get; set; }

        [JsonProperty("merchantIdentifier")]
        public string merchantIdentifier { get; set; }

        [JsonProperty("domainName")]
        public string domainName { get; set; }

        [JsonProperty("displayName")]
        public string displayName { get; set; }

        [JsonProperty("signature")]
        public string signature { get; set; }
    }
}
