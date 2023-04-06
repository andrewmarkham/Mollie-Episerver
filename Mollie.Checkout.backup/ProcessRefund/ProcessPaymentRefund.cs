using System.Net.Http;
using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Security;
using Mollie.Api.Models;
using Mollie.Api.Models.Refund;
using Mollie.Checkout.Helpers;
using Mollie.Checkout.MollieClients;
using Mollie.Checkout.ProcessCheckout;
using Mollie.Checkout.ProcessRefund.Interfaces;
using Mollie.Checkout.Services;
using Mollie.Checkout.Services.Interfaces;

namespace Mollie.Checkout.ProcessRefund
{
    [ServiceConfiguration(typeof(IProcessPaymentRefund))]
    public class ProcessPaymentRefund : IProcessPaymentRefund
    {
        private readonly IReturnOrderFormFinder _returnOrderFormFinder;
        private readonly IMollieRefundClient _mollieRefundClient;
        private readonly IOrderRepository _orderRepository;
        private readonly HttpClient _httpClient;
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly IOrderNoteHelper _orderNoteHelper;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(ProcessOrderCheckout));

        public ProcessPaymentRefund(
            IReturnOrderFormFinder returnOrderFormFinder,
            IMollieRefundClient mollieRefundClient,
            IOrderRepository orderRepository,
            HttpClient httpClient,
            ICheckoutConfigurationLoader checkoutConfigurationLoader,
            IOrderNoteHelper orderNoteHelper)
        {
            _returnOrderFormFinder = returnOrderFormFinder;
            _mollieRefundClient = mollieRefundClient;
            _orderRepository = orderRepository;
            _httpClient = httpClient;
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
            _orderNoteHelper = orderNoteHelper;
        }

        public PaymentProcessingResult Process(IOrderGroup orderGroup, IPayment payment)
        {
            if (!(orderGroup is IPurchaseOrder purchaseOrder))
            {
                return PaymentProcessingResult.CreateUnsuccessfulResult("--Mollie Refund Payment is not successful. Order is not of expected type IPurchaseOrder.");
            }

            var languageId = payment.Properties[Constants.OtherPaymentFields.LanguageId] as string;

            var checkoutConfiguration = _checkoutConfigurationLoader.GetConfiguration(languageId);
            var paymentId = payment.Properties[Constants.OtherPaymentFields.MolliePaymentId] as string;
            var returnForm = _returnOrderFormFinder.Find(purchaseOrder);

            var refundResponse = _mollieRefundClient.CreateRefundAsync(
                paymentId,
                new RefundRequest
                {
                    Amount = new Amount(orderGroup.Currency.CurrencyCode, payment.Amount),
                    Description = returnForm?.ReturnComment ?? "Not set"
                },
                checkoutConfiguration.ApiKey, 
                _httpClient)
                .GetAwaiter().GetResult();

            var message = $"--Mollie Refund Payment is successful. Refunded {refundResponse.Amount}, status {refundResponse.Status}.";

            _orderNoteHelper.AddNoteToOrder(orderGroup, "Mollie Payment refund", message, PrincipalInfo.CurrentPrincipal.GetContactId());

            _orderRepository.Save(orderGroup);
            _logger.Information(message);

            return PaymentProcessingResult.CreateSuccessfulResult(message);
        }
    }
}
