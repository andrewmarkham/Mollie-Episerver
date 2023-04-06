using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Order;
using Mediachase.Commerce;

namespace Mollie.Checkout.Helpers
{
    public interface ILineItemCalculations
    {
        Money GetSalesTax(
            ILineItem lineItem,
            IMarket market,
            Currency currency,
            IOrderAddress shippingAddress);

        EntryContentBase GetEntryContent(ILineItem lineItem);

        LineItemPrices GetLineItemPrices(
            ILineItem lineItem,
            Currency currency);

        decimal GetEntryDiscount(ILineItem lineItem);
    }
}
