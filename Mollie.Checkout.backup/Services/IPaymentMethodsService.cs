using System.Collections.Generic;
using System.Threading.Tasks;
using Mediachase.Commerce;
using Mollie.Api.Models.PaymentMethod;

namespace Mollie.Checkout.Services
{
    public interface IPaymentMethodsService
    {
        Task<bool> PaymentMethodActiveAsync(
            string paymentMethodId,
            string marketId,
            string languageId,
            Money cartTotal,
            string countryCode);

        Task<List<Models.PaymentMethod>> LoadMethods(
            string languageId);

        Task<List<Models.PaymentMethod>> LoadMethods(
            string marketId,
            string languageId,
            string countryCode);

        Task<List<Models.PaymentMethod>> LoadMethods(
            string marketId,
            string languageId, 
            Money cartTotal, 
            string countryCode);

        Task<List<PaymentMethodResponse>> LoadMethods(
            string languageId,
            Currency currency,
            decimal amount,
            string countryCode,
            string apiKey,
            bool useOrderApi,
            bool includeIssuers);

        IEnumerable<KeyValuePair<string, string>> GetCurrencyValidationIssues(
            string languageId,
            string countryCode,
            string apiKey,
            bool useOrdersApi,
            IMarket market);
    }
}
