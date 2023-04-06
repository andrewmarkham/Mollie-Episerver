using System.Net.Http;
using System.Threading.Tasks;
using EPiServer.ServiceLocation;
using Mollie.Api.Client;
using Mollie.Api.Models.Refund;

namespace Mollie.Checkout.MollieClients
{
    [ServiceConfiguration(typeof(IMollieRefundClient))]
    public class MollieRefundClient : IMollieRefundClient
    {
        public Task<RefundResponse> CreateRefundAsync(
            string paymentId,
            RefundRequest refundRequest,
            string apiKey,
            HttpClient httpClient)
        {
            var refundClient = new RefundClient(apiKey, httpClient);

            return refundClient.CreateRefundAsync(paymentId, refundRequest);
        }
    }
}
