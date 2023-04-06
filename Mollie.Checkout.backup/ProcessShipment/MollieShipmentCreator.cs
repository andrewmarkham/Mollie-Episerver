using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Helpers;
using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mollie.Api.Models.Order;
using Mollie.Api.Models.Shipment;
using Mollie.Checkout.ProcessShipment.Interfaces;
using Mollie.Checkout.Services;

namespace Mollie.Checkout.ProcessShipment
{
    [ServiceConfiguration(typeof(IMollieShipmentCreator))]
    public class MollieShipmentCreator : IMollieShipmentCreator
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(MollieShipmentCreator));
        private readonly HttpClient _httpClient;
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;

        public MollieShipmentCreator(
            HttpClient httpClient,
            ICheckoutConfigurationLoader checkoutConfigurationLoader)
        {
            _httpClient = httpClient;
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
        }

        public void Create(
            IPurchaseOrder purchaseOrder,
            List<IShipment> shipments)
        {
            var mollieOrderId = purchaseOrder.Properties[Constants.MollieOrder.OrderIdMollie] as string;
            if (string.IsNullOrWhiteSpace(mollieOrderId))
            {
                return;
            }

            var languageId = purchaseOrder.Properties[Constants.MollieOrder.LanguageId] as string;
            var checkoutConfiguration = _checkoutConfigurationLoader.GetConfiguration(languageId);
            var orderClient = new Api.Client.OrderClient(checkoutConfiguration.ApiKey);

            var mollieOrder = orderClient.GetOrderAsync(mollieOrderId).GetAwaiter().GetResult();
            if (mollieOrder?.Lines == null || !mollieOrder.Lines.Any())
            {
                _logger.Log(Level.Information, $"Mollie order not found for EPiServer order {purchaseOrder.OrderNumber}.");
                return;
            }

            var shipmentTrackingNumber = shipments.FirstOrDefault()?.ShipmentTrackingNumber;
            var shippingMethodName = shipments.FirstOrDefault()?.ShippingMethodName;
            if (string.IsNullOrWhiteSpace(shipmentTrackingNumber))
            {
                _logger.Log(Level.Information, $"No tracking available for EPiServer order {purchaseOrder.OrderNumber}.");
                shipmentTrackingNumber = "No Tracking Available";
            }

            var shipmentRequest = new ShipmentRequest
            {
                Tracking = new TrackingObject
                {
                    Carrier = shippingMethodName,
                    Code = shipmentTrackingNumber
                },
                Lines = GetShipmentLines(
                    purchaseOrder, 
                    shipments,
                    mollieOrder.Lines)
            };

            var shipmentClient = new Api.Client.ShipmentClient(checkoutConfiguration.ApiKey, _httpClient);

            try
            {
                shipmentClient.CreateShipmentAsync(mollieOrderId, shipmentRequest)
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.Log(Level.Error, $"Mollie shipment API throws an error {ex.Message}.");
                throw;
            }
        }

        private IEnumerable<ShipmentLineRequest> GetShipmentLines(
            IPurchaseOrder purchaseOrder,
            IReadOnlyCollection<IShipment> shipments,
            IEnumerable<OrderLineResponse> mollieOrderLines)
        {
            foreach (var mollieOrderLine in mollieOrderLines)
            {
                if (string.IsNullOrWhiteSpace(mollieOrderLine.Metadata))
                {
                    _logger.Log(Level.Error, $"Metadata missing for Mollie order line {mollieOrderLine.Id}.");
                    continue;
                }

                var metadata = Json.Decode(mollieOrderLine.Metadata);
                string orderNumber = metadata.order_id;
                string code = metadata.line_code;

                if (purchaseOrder.OrderNumber != orderNumber)
                {
                    _logger.Log(Level.Error, $"EPiServer order {purchaseOrder.OrderNumber} does not match order number in Mollie order line metadata {orderNumber}.");
                    continue;
                }

                if (code == "shipment")
                {
                    var shippingTotal = purchaseOrder.GetShippingTotal().Amount;

                    yield return new ShipmentLineRequest
                    {
                        Id = mollieOrderLine.Id,
                        Amount = new Api.Models.Amount(purchaseOrder.Currency.CurrencyCode, shippingTotal),
                        Quantity = 1
                    };
                }
                else
                {
                    foreach (var shipment in shipments)
                    {
                        var lineItem = shipment.LineItems.FirstOrDefault(l => l.Code == code);
                        if (lineItem == null)
                        {
                            _logger.Log(Level.Information, $"Line item with code {code} not found in EPiServer order {purchaseOrder.OrderNumber}.");
                            continue;
                        }

                        yield return new ShipmentLineRequest
                        {
                            Id = mollieOrderLine.Id,
                            Amount = new Api.Models.Amount(purchaseOrder.Currency.CurrencyCode, lineItem.GetLineItemPrices(purchaseOrder.Currency).DiscountedPrice),
                            Quantity = (int)lineItem.Quantity
                        };
                    }
                }
            }
        }
    }
}
