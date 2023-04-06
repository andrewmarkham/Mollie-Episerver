using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Mollie.Api.Models.Payment.Response;
using Newtonsoft.Json;
using System;
using Mollie.Checkout.Services.Interfaces;
using Mollie.Checkout.Helpers;
using static Mollie.Checkout.Constants;

namespace Mollie.Checkout.Services
{
    [ServiceConfiguration(typeof(IOrderGroupPaymentService))]
    public class OrderGroupPaymentService : IOrderGroupPaymentService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IMollieCheckoutService _mollieCheckoutService;
        private readonly IOrderNoteHelper _orderNoteHelper;

        public OrderGroupPaymentService(
            IOrderRepository orderRepository,
            IMollieCheckoutService mollieCheckoutService,
            IOrderNoteHelper orderNoteHelper)
        {
            _orderRepository = orderRepository;
            _mollieCheckoutService = mollieCheckoutService;
            _orderNoteHelper = orderNoteHelper;
        }

        public void UpdateStatus(IOrderGroup orderGroup, IPayment orderGroupPayment, PaymentResponse paymentResponse)
        {
            if(orderGroup == null)
            {
                throw new ArgumentNullException(nameof(orderGroup));
            }

            if (orderGroupPayment == null)
            {
                throw new ArgumentNullException(nameof(orderGroupPayment));
            }

            if (paymentResponse == null)
            {
                throw new ArgumentNullException(nameof(paymentResponse));
            }

            orderGroupPayment.ProviderTransactionID = paymentResponse.Id;

            // Store Mollie Payment Status
            if (orderGroupPayment.Properties.ContainsKey(OtherPaymentFields.MolliePaymentStatus))
            {
                orderGroupPayment.Properties[OtherPaymentFields.MolliePaymentStatus] = paymentResponse.Status;
            }
            else
            {
                orderGroupPayment.Properties.Add(OtherPaymentFields.MolliePaymentStatus, paymentResponse.Status);
            }

            orderGroupPayment.Properties[OtherPaymentFields.MolliePaymentMethod] = paymentResponse.Method;
            orderGroupPayment.Properties[OtherPaymentFields.MolliePaymentFullResult] = JsonConvert.SerializeObject(paymentResponse);

            // Add Note to the Order
            _orderNoteHelper.AddNoteToOrder(orderGroup, "Mollie Payment Update",
                $"Mollie Payment Update received. New Status is {paymentResponse.Status}", Guid.Empty);

            switch (paymentResponse.Status)
            {
                case MolliePaymentStatus.Open:
                case MolliePaymentStatus.Pending:
                    orderGroupPayment.Status = PaymentStatus.Pending.ToString();
                    _orderRepository.Save(orderGroup);
                    break;
                case MolliePaymentStatus.Paid:
                case MolliePaymentStatus.Authorized:
                    orderGroupPayment.Status = PaymentStatus.Processed.ToString();
                    _orderRepository.Save(orderGroup);

                    _mollieCheckoutService.HandlePaymentSuccess(orderGroup, orderGroupPayment);
                    break;
                case MolliePaymentStatus.Canceled:
                case MolliePaymentStatus.Expired:
                case MolliePaymentStatus.Failed:
                    orderGroupPayment.Status = PaymentStatus.Failed.ToString();
                    _orderRepository.Save(orderGroup);

                    _mollieCheckoutService.HandlePaymentFailure(orderGroup, orderGroupPayment);
                    break;
                default:
                    break;
            }            
        }
    }
}
