using EPiServer.Commerce.Order;
using EPiServer.Logging;
using Mollie.Api.Client;
using Mollie.Api.Models.Payment.Response;
using Mollie.Checkout.Models;
using Mollie.Checkout.Services;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;

namespace Mollie.Checkout.Webhooks
{
    [RoutePrefix(Constants.Webhooks.MolliePaymentsWebhookUrl)]
    public class MolliePaymentsWebhookApiController : ApiController
    {
        private readonly ILogger _log = LogManager.GetLogger(typeof(MolliePaymentsWebhookApiController));
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly IOrderRepository _orderRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IOrderGroupPaymentService _orderGroupPaymentService;
        private readonly HttpClient _httpClient;

        public MolliePaymentsWebhookApiController(
            ICheckoutConfigurationLoader checkoutConfigurationLoader,
            IOrderRepository orderRepository,
            IPurchaseOrderRepository purchaseOrderRepository,
            IOrderGroupPaymentService orderGroupPaymentService,
            HttpClient httpClient)
        {
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
            _orderRepository = orderRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _orderGroupPaymentService = orderGroupPaymentService;
            _httpClient = httpClient;
        }

        [HttpGet]
        [Route("isonline")]
        public string IsOnline()
        {
            return "Payments Webhook is Online!";
        }

        [HttpPost]
        [Route("{languageId}")]
        public async Task<IHttpActionResult> IndexAsync(string languageId)
        {
            var jsonResult = Request.Content.ReadAsStringAsync().Result;

            if(string.IsNullOrWhiteSpace(jsonResult))
            {
                _log.Error($"There is no Result from the Mollie Payments API.");

                return Ok();
            }

            var molliePaymentId = Regex.Match(jsonResult, @"id=(\w+)").Groups[1].Value;

            // Get Configuration
            var config = _checkoutConfigurationLoader.GetConfiguration(languageId);

            if(config == null)
            {
                _log.Error($"Configuration with LanguageID {languageId} has no config.");

                return Ok();
            }

            // Get Payment Client with API Key
            var paymentClient = new PaymentClient(config.ApiKey, _httpClient);

            // Get Payment from Mollie with API Key
            var paymentResponse = await paymentClient.GetPaymentAsync(molliePaymentId);
            
            if(paymentResponse == null)
            {
                _log.Error($"Mollie Payment with ID {molliePaymentId} has no result.");

                return Ok();
            }

            CheckoutMetaDataModel metaDataResponse = paymentResponse.GetMetadata<CheckoutMetaDataModel>();

            if (metaDataResponse == null)
            {
                _log.Error($"There is no Metadata available.");

                return Ok();
            }

            IOrderGroup orderGroup = _purchaseOrderRepository.Load(metaDataResponse.OrderNumber);
            if (orderGroup == null)
            {
                orderGroup = _orderRepository.Load<ICart>(metaDataResponse.OrderGroupId);
            }

            if (orderGroup == null)
            {
                _log.Error($"PurchaseOrder with Number {metaDataResponse.OrderNumber} Or Cart with ID {metaDataResponse.OrderGroupId} does not exist.");

                return Ok();
            }

            var orderGroupPayments = orderGroup.GetFirstForm().Payments;

            // Update Payments
            foreach (var orderGroupPayment in orderGroupPayments)
            {
                await HandlePaymentUpdateAsync(_orderGroupPaymentService, orderGroup, orderGroupPayment, paymentResponse);
            }

            return Ok();
        }

        private async Task HandlePaymentUpdateAsync(
            IOrderGroupPaymentService orderGroupPaymentService,
            IOrderGroup orderGroup,
            IPayment payment,
            PaymentResponse paymentResponse)
        {
            try
            {
                await Task.Factory.StartNew(() =>
                {
                    orderGroupPaymentService.UpdateStatus(orderGroup, payment, paymentResponse);
                    return true;
                });
            }
            catch(Exception ex)
            {
                _log.Error("Error handling Payment Update", ex);
            }
        }
    }
}
