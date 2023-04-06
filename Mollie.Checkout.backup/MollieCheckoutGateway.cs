using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Plugins.Payment;
using System;
using Mollie.Checkout.ProcessCheckout.Helpers.Interfaces;
using Mollie.Checkout.ProcessRefund.Interfaces;

namespace Mollie.Checkout
{
    public class MollieCheckoutGateway : AbstractPaymentGateway, IPaymentPlugin
    {
        private readonly IProcessPaymentRefund _processPaymentRefund;
        private readonly IProcessCheckoutFactory _processCheckoutFactory;

        public MollieCheckoutGateway()
            : this(ServiceLocator.Current.GetInstance<IProcessCheckoutFactory>(),
                ServiceLocator.Current.GetInstance<IProcessPaymentRefund>())
        { }

        public MollieCheckoutGateway(
            IProcessCheckoutFactory processCheckoutFactory,
            IProcessPaymentRefund processPaymentRefund)
        {
            _processCheckoutFactory = processCheckoutFactory;
            _processPaymentRefund = processPaymentRefund;
        }

        /// <summary>
        /// Commerce 10 implementation
        /// </summary>
        public override bool ProcessPayment(Payment payment, ref string message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Commerce 11+ implementation
        /// </summary>
        public PaymentProcessingResult ProcessPayment(IOrderGroup orderGroup, IPayment payment)
        {
            if (null == orderGroup)
            {
                throw new ArgumentNullException(nameof(orderGroup));
            }

            if (!payment.Properties.ContainsKey(Constants.OtherPaymentFields.LanguageId) || 
                string.IsNullOrWhiteSpace(payment.Properties[Constants.OtherPaymentFields.LanguageId] as string))
            {
                throw new Exception("Payment propery LanguageId is not set");
            }
            
            var cart = orderGroup as ICart;

            // The order which is created by Commerce Manager
            if (cart == null && orderGroup is IPurchaseOrder)
            {
                if (payment.TransactionType == TransactionType.Capture.ToString() 
                    || payment.TransactionType == TransactionType.Sale.ToString())
                {
                    return ProcessPaymentCapture(orderGroup, payment);
                }

                // When "Refund" shipment in Commerce Manager, this method will be invoked with the TransactionType is Credit
                if (payment.TransactionType == TransactionType.Credit.ToString())
                {
                    return ProcessPaymentRefund(orderGroup, payment);
                }

                // Right now we do not support processing the order which is created by Commerce Manager
                return PaymentProcessingResult.CreateUnsuccessfulResult("The current payment method does not support order type.");
            }

            // CHECKOUT
            var languageId = payment.Properties[Constants.OtherPaymentFields.LanguageId] as string;
            var processCheckout = _processCheckoutFactory.GetInstance(languageId);

            return processCheckout.Process(cart, payment);
        }

        private PaymentProcessingResult ProcessPaymentRefund(IOrderGroup orderGroup, IPayment payment)
        {
            return _processPaymentRefund.Process(orderGroup, payment);
        }

        private PaymentProcessingResult ProcessPaymentCapture(IOrderGroup orderGroup, IPayment payment)
        {
            // CHECKOUT
            var languageId = payment.Properties[Constants.OtherPaymentFields.LanguageId] as string;
            var processCheckout = _processCheckoutFactory.GetInstance(languageId);

            return processCheckout.Process(orderGroup, payment);
        }
    }
}
