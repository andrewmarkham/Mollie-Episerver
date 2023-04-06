using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Mollie.Api.Client;
using Mollie.Api.Extensions;
using Mollie.Api.Models;
using Mollie.Api.Models.List;
using Mollie.Api.Models.Payment;
using Mollie.Api.Models.PaymentMethod;

namespace Mollie.Checkout.MollieApi
{
    public class MolliePaymentMethodClient : PaymentMethodClient
    {
        public MolliePaymentMethodClient(string apiKey, HttpClient httpClient = null)
            : base(apiKey, httpClient)
        { }

        public async Task<ListResponse<PaymentMethodResponse>> GetPaymentMethodListAsync(
            string sequenceType = null, string locale = null, Amount amount = null, bool includeIssuers = false,
            bool includePricing = false, string profileId = null, bool testmode = false, Resource? resource = null,
            string billingCountry = null)
        {
            Dictionary<string, string> queryParameters = this.BuildQueryParameters(
                sequenceType: sequenceType,
                locale: locale,
                amount: amount,
                includeIssuers: includeIssuers,
                includePricing: includePricing,
                resource: resource,
                profileId: profileId,
                testmode: testmode,
                billingCountry: billingCountry);

            return await this.GetListAsync<ListResponse<PaymentMethodResponse>>("methods", null, null, queryParameters)
                .ConfigureAwait(false);
        }

        private Dictionary<string, string> BuildQueryParameters(string sequenceType = null, string locale = null,
            Amount amount = null, bool includeIssuers = false, bool includePricing = false, string profileId = null,
            bool testmode = false, Resource? resource = null, string currency = null, string billingCountry = null)
        {
            var result = new Dictionary<string, string>();
            result.AddValueIfTrue(nameof(testmode), testmode);
            result.AddValueIfNotNullOrEmpty(nameof(sequenceType), sequenceType?.ToLower());
            result.AddValueIfNotNullOrEmpty(nameof(profileId), profileId);
            result.AddValueIfNotNullOrEmpty(nameof(locale), locale);
            result.AddValueIfNotNullOrEmpty("amount[currency]", amount?.Currency);
            result.AddValueIfNotNullOrEmpty("amount[value]", amount?.Value);
            result.AddValueIfNotNullOrEmpty("include", this.BuildIncludeParameter(includeIssuers, includePricing));
            result.AddValueIfNotNullOrEmpty(nameof(resource), resource?.ToString()?.ToLower());
            result.AddValueIfNotNullOrEmpty(nameof(currency), currency);
            result.AddValueIfNotNullOrEmpty(nameof(billingCountry), billingCountry);
            result.AddValueIfNotNullOrEmpty("includeWallets", "applepay");

            return result;
        }

        private string BuildIncludeParameter(bool includeIssuers = false, bool includePricing = false)
        {
            var includeList = new List<string>();
            includeList.AddValueIfTrue("issuers", includeIssuers);
            includeList.AddValueIfTrue("pricing", includePricing);
            return includeList.ToIncludeParameter();
        }
    }
}
