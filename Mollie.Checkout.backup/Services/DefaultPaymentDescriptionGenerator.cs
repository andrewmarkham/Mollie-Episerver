using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;

namespace Mollie.Checkout.Services
{
    [ServiceConfiguration(typeof(IPaymentDescriptionGenerator))]
    public class DefaultPaymentDescriptionGenerator : IPaymentDescriptionGenerator
    {
        public string GetDescription(IOrderGroup orderGroup, IPayment payment)
        {
            return $"Payment for order {orderGroup.OrderNumber()}";
        }
    }
}
