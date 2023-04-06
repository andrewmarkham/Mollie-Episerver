using EPiServer.Commerce.Catalog.ContentTypes;

namespace Mollie.Checkout.ProcessCheckout.Helpers.Interfaces
{
    public interface IProductUrlGetter
    {
        string Get(EntryContentBase entry);
    }
}
