using EPiServer.Commerce.Order;
using Mollie.Api.Models.Payment.Response;

namespace Mollie.Checkout.Services
{
    public interface IOrderGroupPaymentService
    {
        void UpdateStatus(IOrderGroup orderGroup, IPayment orderGroupPayment, PaymentResponse paymentResponse);
    }
}