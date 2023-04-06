using System.Linq;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Mediachase.MetaDataPlus;

namespace Mollie.Checkout.Helpers
{
    [ServiceConfiguration(typeof(IReturnOrderFormFinder))]
    public class ReturnOrderFormFinder : IReturnOrderFormFinder
    {
        public IReturnOrderForm Find(IPurchaseOrder purchaseOrder)
        {
            //TODO:Find better way to find current return form
            return purchaseOrder.ReturnForms.FirstOrDefault(rf => ((OrderForm)rf).ObjectState == MetaObjectState.Modified);
        }
    }
}
