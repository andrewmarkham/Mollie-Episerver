using EPiServer.Commerce.Order;

namespace Mollie.Checkout.ProcessRefund.Interfaces
{
    public interface IProcessPaymentRefund
    {
        PaymentProcessingResult Process(IOrderGroup orderGroup, IPayment payment);
    }
}
