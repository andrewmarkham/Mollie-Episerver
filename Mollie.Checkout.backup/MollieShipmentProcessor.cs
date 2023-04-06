using System.Collections.Generic;
using System.Linq;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mollie.Checkout.ProcessShipment.Interfaces;

namespace Mollie.Checkout
{
    public class MollieShipmentProcessor : IShipmentProcessor
    {
        private readonly IShipmentProcessor _defaultShipmentProcessor;
        private readonly IMollieShipmentCreator _mollieShipmentCreator;

        public MollieShipmentProcessor(IShipmentProcessor defaultShipmentProcessor)
        {
            _defaultShipmentProcessor = defaultShipmentProcessor;
            _mollieShipmentCreator = ServiceLocator.Current.GetInstance<IMollieShipmentCreator>();
        }

        public OrderProcessingResult CancelShipment(IPurchaseOrder purchaseOrder, IShipment shipment)
        {
            return _defaultShipmentProcessor.CancelShipment(purchaseOrder, shipment);
        }

        public OrderProcessingResult CompleteShipment(IPurchaseOrder purchaseOrder, IEnumerable<IShipment> shipments)
        {
            var shipmentsList = shipments.ToList();
            var orderProcessingResult = _defaultShipmentProcessor.CompleteShipment(purchaseOrder, shipmentsList);

            _mollieShipmentCreator.Create(purchaseOrder, shipmentsList);

            return orderProcessingResult;
        }

        public OrderProcessingResult ReleaseShipment(IPurchaseOrder purchaseOrder, IEnumerable<IShipment> shipments)
        {
            return _defaultShipmentProcessor.ReleaseShipment(purchaseOrder, shipments);
        }

        public OrderProcessingResult AddShipmentToPicklist(IPurchaseOrder purchaseOrder, IShipment shipment, int pickListId)
        {
            return _defaultShipmentProcessor.AddShipmentToPicklist(purchaseOrder, shipment, pickListId);
        }

        public OrderProcessingResult RemoveShipmentFromPicklist(IPurchaseOrder purchaseOrder, IShipment shipment)
        {
            return _defaultShipmentProcessor.RemoveShipmentFromPicklist(purchaseOrder, shipment);
        }

        public void CapturePayment(IOrderGroup order, IShipment shipment)
        {
            _defaultShipmentProcessor.CapturePayment(order, shipment);
        }
    }
}
