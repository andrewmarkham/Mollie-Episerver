using System.Net.Http;
using System.Threading.Tasks;
using EPiServer.ServiceLocation;
using Mollie.Api.Client;
using Mollie.Api.Models.Order;
using Mollie.Api.Models.Payment.Request;
using Mollie.Api.Models.Payment.Response;

namespace Mollie.Checkout.MollieClients
{
    [ServiceConfiguration(typeof(IMollieOrderClient))]
    public class MollieOrderClient : IMollieOrderClient
    {
        public Task<PaymentResponse> CreatePaymentAsync(
            PaymentRequest paymentRequest, 
            string apiKey)
        {
            var paymentClient = new PaymentClient(apiKey);

            return paymentClient.CreatePaymentAsync(paymentRequest);
        }

        public Task<OrderResponse> CreateOrderAsync(
            OrderRequest orderRequest, 
            string apiKey)
        {
            var orderClient = new OrderClient(apiKey);

            return orderClient.CreateOrderAsync(orderRequest);
        }

        public Task<OrderResponse> GetOrderAsync(
            string orderId, 
            string apiKey)
        {
            var orderClient = new OrderClient(apiKey);
            return orderClient.GetOrderAsync(orderId, true);
        }
    }
}
