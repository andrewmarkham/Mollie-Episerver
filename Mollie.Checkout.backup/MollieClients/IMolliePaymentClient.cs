using System.Net.Http;
using System.Threading.Tasks;
using Mollie.Api.Models.Payment.Request;
using Mollie.Api.Models.Payment.Response;

namespace Mollie.Checkout.MollieClients
{
    public interface IMolliePaymentClient
    {
        Task<PaymentResponse> CreatePaymentAsync(
            PaymentRequest paymentRequest,
            string apiKey,
            HttpClient httpClient);
    }
}
