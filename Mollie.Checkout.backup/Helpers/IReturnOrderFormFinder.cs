using EPiServer.Commerce.Order;

namespace Mollie.Checkout.Helpers
{
    public interface IReturnOrderFormFinder
    {
        IReturnOrderForm Find(IPurchaseOrder purchaseOrder);
    }
}
