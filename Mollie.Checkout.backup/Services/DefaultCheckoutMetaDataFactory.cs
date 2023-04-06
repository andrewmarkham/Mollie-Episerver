using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mollie.Checkout.Models;

namespace Mollie.Checkout.Services
{
    [ServiceConfiguration(typeof(ICheckoutMetaDataFactory))]
    public class DefaultCheckoutMetaDataFactory : ICheckoutMetaDataFactory
    {
        public CheckoutMetaDataModel Create(IOrderGroup orderGroup, IPayment payment, CheckoutConfiguration configuration)
        {
            var metaDataObject = new CheckoutMetaDataModel()
            {
                OrderGroupId = orderGroup.OrderLink.OrderGroupId,
                OrderNumber = orderGroup.OrderNumber(),
                VersionStrings = configuration.VersionStrings
            };

            return metaDataObject;
        }
    }
}
