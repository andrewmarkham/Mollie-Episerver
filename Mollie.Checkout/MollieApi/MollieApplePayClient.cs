using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using EPiServer.ServiceLocation;
using Microsoft.AspNetCore.Http;
using Mollie.Api.Client;
using Mollie.Checkout.Helpers;
using Newtonsoft.Json;

namespace Mollie.Checkout.MollieApi
{
    public class MollieApplePayClient : BaseMollieClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MollieApplePayClient(string apiKey, IHttpContextAccessor httpContextAccessor)
            : base(apiKey)
        {

            _httpContextAccessor = httpContextAccessor;
        }

        public ReponseValidateMerchant ValidateMerchant(string validationUrl)
        {
            var request = _httpContextAccessor?.HttpContext?.Request;

            var data = new Dictionary<string, string>
            {
                { "validationUrl", validationUrl },
                { "domain", request.Host.Host }
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
