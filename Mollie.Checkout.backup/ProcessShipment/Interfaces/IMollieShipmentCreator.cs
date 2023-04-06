using System.Collections.Generic;
using EPiServer.Commerce.Order;

namespace Mollie.Checkout.ProcessShipment.Interfaces
{
    public interface IMollieShipmentCreator
    {
        void Create(
            IPurchaseOrder purchaseOrder,
            List<IShipment> shipments);
    }
}
