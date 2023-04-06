using System.Net.Http;
using System.Threading.Tasks;
using EPiServer.ServiceLocation;
using Mollie.Api.Client;
using Mollie.Api.Models.Payment.Request;
using Mollie.Api.Models.Payment.Response;

namespace Mollie.Checkout.MollieClients
{
    [ServiceConfiguration(typeof(IMolliePaymentClient))]
    public class MolliePaymentClient : IMolliePaymentClient
    {
        public Task<PaymentResponse> CreatePaymentAsync(
            PaymentRequest paymentRequest, 
            string apiKey)
        {
            var paymentClient = new PaymentClient(apiKey,null /*httpClient)*/);

            return paymentClient.CreatePaymentAsync(paymentRequest);
        }
    }
}
