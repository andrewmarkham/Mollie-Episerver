using EPiServer.Commerce.Order;

namespace Mollie.Checkout.Services
{
    public interface IPaymentDescriptionGenerator
    {
        string GetDescription(IOrderGroup orderGroup, IPayment payment);
    }
}
