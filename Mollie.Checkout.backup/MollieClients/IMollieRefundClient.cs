using System.Net.Http;
using System.Threading.Tasks;
using Mollie.Api.Models.Refund;

namespace Mollie.Checkout.MollieClients
{
    public interface IMollieRefundClient
    {
        Task<RefundResponse> CreateRefundAsync(
            string paymentId,
            RefundRequest refundRequest,
            string apiKey,
            HttpClient httpClient);
    }
}
