using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;

namespace Mollie.Checkout.Helpers
{
    [ServiceConfiguration(typeof(ILineItemCalculations))]
    public class LineItemItemCalculations : ILineItemCalculations
    {
        public Money GetSalesTax(
            ILineItem lineItem,
            IMarket market,
            Currency currency,
            IOrderAddress shippingAddress)
        {
            return lineItem.GetSalesTax(market, currency, shippingAddress);
        }

        public EntryContentBase GetEntryContent(ILineItem lineItem)
        {
            return lineItem.GetEntryContent();
        }

        public LineItemPrices GetLineItemPrices(
            ILineItem lineItem,
            Currency currency)
        {
            return lineItem.GetLineItemPrices(currency);
        }

        public decimal GetEntryDiscount(ILineItem lineItem)
        {
            return lineItem.GetEntryDiscount();
        }
    }
}
