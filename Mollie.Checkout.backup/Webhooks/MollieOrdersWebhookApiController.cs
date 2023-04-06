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
    [RoutePrefix(Constants.Webhooks.MollieOrdersWebhookUrl)]
    public class MollieOrdersWebhookApiController : ApiController
    {
        private readonly ILogger _log = LogManager.GetLogger(typeof(MollieOrdersWebhookApiController));
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderGroupPaymentService _orderGroupPaymentService;
        private readonly IMollieCheckoutService _mollieCheckoutService;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly HttpClient _httpClient;

        public MollieOrdersWebhookApiController(
            ICheckoutConfigurationLoader checkoutConfigurationLoader,
            IOrderRepository orderRepository,
            IOrderGroupPaymentService orderGroupPaymentService,
            IMollieCheckoutService mollieCheckoutService,
            IPurchaseOrderRepository purchaseOrderRepository,
            HttpClient httpClient)
        {
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
            _orderRepository = orderRepository;
            _orderGroupPaymentService = orderGroupPaymentService;
            _mollieCheckoutService = mollieCheckoutService;
            _purchaseOrderRepository = purchaseOrderRepository;
            _httpClient = httpClient;
        }

        [HttpGet]
        [Route("isonline")]
        public string IsOnline()
        {
            return "Orders Webhook is Online!";
        }

        [HttpPost]
        [Route("{languageId}")]
        public async Task<IHttpActionResult> IndexAsync(string languageId)
        {
            var jsonResult = Request.Content.ReadAsStringAsync().Result;

            if (string.IsNullOrWhiteSpace(jsonResult))
            {
                _log.Error($"There is no Result from the Mollie Orders API.");

                return Ok();
            }

            var mollieOrderId = Regex.Match(jsonResult, @"id=(\w+)").Groups[1].Value;

            // Get Configuration
            var config = _checkoutConfigurationLoader.GetConfiguration(languageId);

            if (config == null)
            {
                _log.Error($"Configuration with LanguageID {languageId} has no config.");

                return Ok();
            }

            // Get Order Client with API Key
            var orderClient = new OrderClient(config.ApiKey, _httpClient);

            // Get Order from Mollie with API Key with Embedded enabled
            var orderResult = await orderClient.GetOrderAsync(mollieOrderId, true, true, true).ConfigureAwait(false);

            if (orderResult == null)
            {
                _log.Error($"Mollie Order with ID {mollieOrderId} has no result.");

                return Ok();
            }

            CheckoutMetaDataModel metaDataResponse = orderResult.GetMetadata<CheckoutMetaDataModel>();

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


            // Update Order/Cart            
            _mollieCheckoutService.HandleOrderStatusUpdate(orderGroup, orderResult.Status, orderResult.Id);

            // Update Payments
            var orderGroupPayments = orderGroup.GetFirstForm().Payments;

            var mollieOrderPayments = orderResult.Embedded?.Payments;

            foreach (var molliePayment in mollieOrderPayments)
            {
                foreach (var orderGroupPayment in orderGroupPayments)
                {
                    await HandlePaymentUpdateAsync(_orderGroupPaymentService, orderGroup, orderGroupPayment, molliePayment);
                }
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
