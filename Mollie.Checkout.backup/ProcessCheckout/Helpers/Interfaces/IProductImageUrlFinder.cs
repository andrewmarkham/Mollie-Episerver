using EPiServer.Commerce.Catalog.ContentTypes;

namespace Mollie.Checkout.ProcessCheckout.Helpers.Interfaces
{
    public interface IProductImageUrlFinder
    {
        string Find(EntryContentBase entry);
    }
}
