using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using EPiServer.Commerce.Order;
using Mollie.Checkout.ProcessCheckout.Interfaces;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mollie.Checkout.Services;
using System.Web;
using EPiServer.Security;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Orders.Managers;
using Mediachase.Commerce.Security;
using Mollie.Api.Models.Order;
using Mollie.Checkout.ProcessCheckout.Helpers.Interfaces;
using Newtonsoft.Json;
using System.Net.Http;
using Mollie.Api.Models;
using Mollie.Checkout.Services.Interfaces;
using Mollie.Api.Models.Order.Request.PaymentSpecificParameters;
using System.Text;
using Mollie.Api.Models.Payment.Response;
using Mollie.Checkout.MollieApi;
using Mollie.Checkout.Helpers;
using Mollie.Checkout.MollieClients;
using System.Runtime.ExceptionServices;

namespace Mollie.Checkout.ProcessCheckout
{
    public class ProcessOrderCheckout : IProcessCheckout
    {
        private readonly ILogger _logger;
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly ICheckoutMetaDataFactory _checkoutMetaDataFactory;
        private readonly IOrderRepository _orderRepository;
        private readonly IMarketService _marketService;
        private readonly ServiceAccessor<HttpContextBase> _httpContextAccessor;
        private readonly IProductImageUrlFinder _productImageUrlFinder;
        private readonly IProductUrlGetter _productUrlGetter;
        private readonly HttpClient _httpClient;
        private readonly IOrderNoteHelper _orderNoteHelper;
        private readonly IMollieOrderClient _mollieOrderClient;
        private readonly ICurrentCustomerContactGetter _currentCustomerContactGetter;
        private readonly ILineItemCalculations _lineItemCalculations;

        public ProcessOrderCheckout()
        {
            _logger = LogManager.GetLogger(typeof(ProcessOrderCheckout));
            _checkoutConfigurationLoader = ServiceLocator.Current.GetInstance<ICheckoutConfigurationLoader>();
            _checkoutMetaDataFactory = ServiceLocator.Current.GetInstance<ICheckoutMetaDataFactory>();
            _orderRepository = ServiceLocator.Current.GetInstance<IOrderRepository>();
            _marketService = ServiceLocator.Current.GetInstance<IMarketService>();
            _productImageUrlFinder = ServiceLocator.Current.GetInstance<IProductImageUrlFinder>();
            _productUrlGetter = ServiceLocator.Current.GetInstance<IProductUrlGetter>();
            _httpContextAccessor = ServiceLocator.Current.GetInstance<ServiceAccessor<HttpContextBase>>();
            _httpClient = ServiceLocator.Current.GetInstance<HttpClient>();
            _orderNoteHelper = ServiceLocator.Current.GetInstance<IOrderNoteHelper>();
            _mollieOrderClient = ServiceLocator.Current.GetInstance<IMollieOrderClient>();
            _currentCustomerContactGetter = ServiceLocator.Current.GetInstance<ICurrentCustomerContactGetter>();
            _lineItemCalculations = ServiceLocator.Current.GetInstance<ILineItemCalculations>();
        }

        public ProcessOrderCheckout(
            ILogger logger,
            ICheckoutConfigurationLoader checkoutConfigurationLoader,
            ICheckoutMetaDataFactory checkoutMetaDataFactory,
            IOrderRepository orderRepository,
            IMarketService marketService,
            ServiceAccessor<HttpContextBase> httpContextAccessor,
            IProductImageUrlFinder productImageUrlFinder,
            IProductUrlGetter productUrlGetter,
            HttpClient httpClient,
            IOrderNoteHelper orderNoteHelper,
            IMollieOrderClient mollieOrderClient,
            ICurrentCustomerContactGetter currentCustomerContactGetter,
            ILineItemCalculations lineItemCalculations)
        {
            _logger = logger;
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
            _checkoutMetaDataFactory = checkoutMetaDataFactory;
            _orderRepository = orderRepository;
            _marketService = marketService;
            _httpContextAccessor = httpContextAccessor;
            _productImageUrlFinder = productImageUrlFinder;
            _productUrlGetter = productUrlGetter;
            _httpClient = httpClient;
            _orderNoteHelper = orderNoteHelper;
            _mollieOrderClient = mollieOrderClient;
            _currentCustomerContactGetter = currentCustomerContactGetter;
            _lineItemCalculations = lineItemCalculations;
        }

        public PaymentProcessingResult Process(IOrderGroup orderGroup, IPayment payment)
        {
            var languageId = payment.Properties[Constants.OtherPaymentFields.LanguageId] as string;

            if (string.IsNullOrWhiteSpace(languageId))
            {
                throw new CultureNotFoundException("Unable to get payment language.");
            }

            var selectedMethod = string.Empty;

            if (payment.Properties.ContainsKey(Constants.OtherPaymentFields.MolliePaymentMethod))
            {
                selectedMethod = payment.Properties[Constants.OtherPaymentFields.MolliePaymentMethod] as string;
            }
            
            var request = _httpContextAccessor().Request;

            var baseUrl = $"{request.Url?.Scheme}://{request.Url?.Authority}";

            var proxyUrl = ConfigurationManager.AppSettings["mollie:webhook.proxyurl"];

            if (!string.IsNullOrWhiteSpace(proxyUrl))
            {
                baseUrl = proxyUrl;
            }

            var urlBuilder = new UriBuilder(baseUrl)
            {
                Path = $"{Constants.Webhooks.MollieOrdersWebhookUrl}/{languageId}"
            };

            var checkoutConfiguration = _checkoutConfigurationLoader.GetConfiguration(languageId);

            if (string.IsNullOrWhiteSpace(checkoutConfiguration?.RedirectUrl))
            {
                throw new ApplicationException("Redirect url configuration not set.");
            }

            if (string.IsNullOrWhiteSpace(checkoutConfiguration.ApiKey))
            {
                throw new ApplicationException("Api key configuration not set.");
            }

            var shipment = orderGroup.GetFirstShipment();
            var billingAddress = payment.BillingAddress;
            var shippingAddress = shipment.ShippingAddress;
            var orderNumber = orderGroup.OrderNumber();
            var currentContact = _currentCustomerContactGetter.Get();

            var orderRequest = new OrderRequest
            {
                Amount = new Amount(orderGroup.Currency.CurrencyCode, payment.Amount),
                Method = selectedMethod,
                BillingAddress = new OrderAddressDetails
                {
                    OrganizationName = billingAddress.Organization,
                    StreetAndNumber = $"{billingAddress.Line1} {billingAddress.Line2}",
                    City = billingAddress.City,
                    Region = billingAddress.RegionName,
                    PostalCode = billingAddress.PostalCode,
                    Country = billingAddress.CountryCode?.Length == 3 ? CountryCodeMapper.MapToTwoLetterIsoRegion(billingAddress.CountryCode) : billingAddress.CountryCode,
                    GivenName = billingAddress.FirstName,
                    FamilyName = billingAddress.LastName,
                    Email = billingAddress.Email,
                    Phone = billingAddress.DaytimePhoneNumber
                },
                ShippingAddress = new OrderAddressDetails
                {
                    OrganizationName = shippingAddress.Organization,
                    StreetAndNumber = $"{shippingAddress.Line1} {shippingAddress.Line2}",
                    City = shippingAddress.City,
                    Region = shippingAddress.RegionName,
                    PostalCode = shippingAddress.PostalCode,
                    Country = shippingAddress.CountryCode?.Length == 3 ? CountryCodeMapper.MapToTwoLetterIsoRegion(shippingAddress.CountryCode) : shippingAddress.CountryCode,
                    GivenName = shippingAddress.FirstName,
                    FamilyName = shippingAddress.LastName,
                    Email = shippingAddress.Email,
                    Phone = shippingAddress.DaytimePhoneNumber
                },
                ConsumerDateOfBirth = currentContact?.BirthDate,
                Locale = LanguageUtils.GetLocale(languageId),
                OrderNumber = orderNumber,
                RedirectUrl = checkoutConfiguration.RedirectUrl + $"?orderNumber={orderNumber}",
                WebhookUrl = urlBuilder.ToString(),
                Lines = GetOrderLines(orderGroup),
                ExpiresAt = DetermineExpiredAt(checkoutConfiguration)
            };

            var metaData = _checkoutMetaDataFactory.Create(orderGroup, payment, checkoutConfiguration);

            orderRequest.SetMetadata(metaData);

            if (!string.IsNullOrWhiteSpace(selectedMethod) && selectedMethod.Equals(Api.Models.Payment.PaymentMethod.Ideal, StringComparison.InvariantCultureIgnoreCase))
            {
                if (payment.Properties.ContainsKey(Constants.OtherPaymentFields.MollieIssuer))
                {
                    var issuer = payment.Properties[Constants.OtherPaymentFields.MollieIssuer] as string;

                    orderRequest.Payment = new IDealSpecificParameters
                    {
                        Issuer = issuer
                    };
                }
            }
            else if (!string.IsNullOrWhiteSpace(selectedMethod) && selectedMethod.Equals(Api.Models.Payment.PaymentMethod.CreditCard, StringComparison.InvariantCultureIgnoreCase))
            {
                if (payment.Properties.ContainsKey(Constants.OtherPaymentFields.MollieToken))
                {
                    var ccToken = payment.Properties[Constants.OtherPaymentFields.MollieToken] as string;

                    orderRequest.Payment = new CreditCardSpecificParameters
                    {
                        CardToken = ccToken
                    };
                }
            }

            OrderResponse createOrderResponse;

            try
            {
                createOrderResponse = _mollieOrderClient.CreateOrderAsync(orderRequest, checkoutConfiguration.ApiKey, _httpClient)
                    .GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                _logger.Error($"Creating Order in Mollie failed for Cart: {orderNumber}", e);

                throw new Exception($"Creating Order in Mollie failed for Cart: {orderNumber} with Message: {e.Message} ", e);
            }

            OrderResponse getOrderResponse;

            try
            {
                getOrderResponse = _mollieOrderClient.GetOrderAsync(createOrderResponse?.Id, checkoutConfiguration.ApiKey, _httpClient)
                    .GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                _logger.Error($"Getting Order from Mollie failed for Cart: {orderNumber}", e);

                throw new Exception($"Getting Order from Mollie failed for Cart: {orderNumber} with Message: {e.Message} ");
            }

            var molliePaymentIdMessage = new StringBuilder();

            foreach (var molliePayment in getOrderResponse?.Embedded?.Payments ?? new List<PaymentResponse>())
            {
                if (payment.Properties.ContainsKey(Constants.OtherPaymentFields.MolliePaymentId))
                {
                    payment.Properties[Constants.OtherPaymentFields.MolliePaymentId] = molliePayment.Id;

                    molliePaymentIdMessage.AppendLine($"Mollie Payment ID updated: {molliePayment?.Id}");
                }
                else
                {
                    payment.Properties.Add(Constants.OtherPaymentFields.MolliePaymentId, molliePayment.Id);

                    molliePaymentIdMessage.AppendLine($"Mollie Payment ID created: {molliePayment?.Id}");
                }
            }

            if (!string.IsNullOrWhiteSpace(molliePaymentIdMessage.ToString()))
            {
                _orderNoteHelper.AddNoteToOrder(orderGroup, "Mollie Payment ID", molliePaymentIdMessage.ToString(), PrincipalInfo.CurrentPrincipal.GetContactId());
            }

            var message = getOrderResponse?.Links.Checkout != null && !string.IsNullOrWhiteSpace(getOrderResponse?.Links.Checkout.Href)
                ? $"Mollie Create Order is successful. Redirect end user to {getOrderResponse?.Links.Checkout.Href}"
                : "Mollie Create Order is successful. No redirect needed";

            orderGroup.Properties[Constants.PaymentLinkMollie] = getOrderResponse?.Links.Checkout?.Href;

            _orderNoteHelper.AddNoteToOrder(orderGroup, "Mollie Order created", message, PrincipalInfo.CurrentPrincipal.GetContactId());

            _orderRepository.Save(orderGroup);

            _logger.Information(message);

            // If no redirect to molly is needed, redirect to the redirect page directly (cart/order updates will be handled by webhook in background)..
            var redirectUrl = getOrderResponse?.Links.Checkout != null && !string.IsNullOrWhiteSpace(getOrderResponse?.Links.Checkout.Href)
                ? getOrderResponse.Links.Checkout.Href
                : orderRequest.RedirectUrl;

            return PaymentProcessingResult.CreateSuccessfulResult(message, redirectUrl);
        }

        private IEnumerable<OrderLineRequest> GetOrderLines(
            IOrderGroup cart)
        {
            var shipment = cart.GetFirstShipment();
            var shippingAddress = shipment.ShippingAddress;
            var orderLines = cart.GetAllLineItems();
            var market = _marketService.GetMarket(cart.MarketId);
            var orderNumber = cart.OrderNumber();

            foreach (var orderLine in orderLines)
            {
                var metadata = new
                {
                    order_id = orderNumber,
                    line_code = orderLine.Code
                };

                //TODO: Check vat rate should be dependent om market
                var varRate = (orderLine.TaxCategoryId == null ? 0 : GetVatRate(orderLine.TaxCategoryId.Value)).ToString("0.00");

                var vatAmount = new Amount(cart.Currency.CurrencyCode, _lineItemCalculations.GetSalesTax(orderLine, market, cart.Currency, shippingAddress).Amount);

                yield return new OrderLineRequest
                {
                    Type = "physical",
                    Sku = orderLine.Code,
                    Name = orderLine.DisplayName,
                    ProductUrl = _productUrlGetter.Get(_lineItemCalculations.GetEntryContent(orderLine)),
                    ImageUrl = _productImageUrlFinder.Find(_lineItemCalculations.GetEntryContent(orderLine)),
                    Quantity = (int) orderLine.Quantity,
                    VatRate = vatAmount > 0 ? varRate : "0.00",
                    UnitPrice = new Amount(cart.Currency.CurrencyCode, orderLine.PlacedPrice),
                    TotalAmount = new Amount(cart.Currency.CurrencyCode, _lineItemCalculations.GetLineItemPrices(orderLine, cart.Currency).DiscountedPrice),
                    DiscountAmount = new Amount(cart.Currency.CurrencyCode, _lineItemCalculations.GetEntryDiscount(orderLine)),
                    //TODO: Why is it returning 0 vat?
                    VatAmount = vatAmount,
                    Metadata = JsonConvert.SerializeObject(metadata),
                    Category = "gift"   
                };
            }

            var shippingTotal = cart.GetShippingTotal().Amount;

            if (shippingTotal > 0)
            {
                var metadata = new
                {
                    order_id = orderNumber,
                    line_code = "shipment"
                };

                yield return new OrderLineRequest
                {
                    Type = "shipping_fee",
                    Name = "Shipping",
                    TotalAmount = new Amount(cart.Currency.CurrencyCode, shippingTotal),
                    UnitPrice = new Amount(cart.Currency.CurrencyCode, shippingTotal),
                    DiscountAmount = new Amount(cart.Currency.CurrencyCode, cart.GetShippingDiscountTotal().Amount),
                    Quantity = 1,
                    VatAmount = new Amount(cart.Currency.CurrencyCode, 0),
                    VatRate = "0",
                    Metadata = JsonConvert.SerializeObject(metadata)
                };
            }

            var orderDiscountTotal = cart.GetOrderDiscountTotal();

            if (orderDiscountTotal > 0)
            {
                yield return new OrderLineRequest
                {
                    Type = "discount",
                    Name = "Order Level Discount",
                    TotalAmount = new Amount(cart.Currency.CurrencyCode, -orderDiscountTotal.Amount),
                    UnitPrice = new Amount(cart.Currency.CurrencyCode, -orderDiscountTotal.Amount),
                    VatAmount = new Amount(cart.Currency.CurrencyCode, 0),
                    Quantity = 1,
                    VatRate = "0"
                };
            }
        }

        private static double GetVatRate(int taxCategoryId)
        {
            var taxDto = TaxManager.GetTax(taxCategoryId);
            var tax = taxDto?.TaxValue?.FirstOrDefault();

            return tax?.Percentage ?? 0;
        }

        private static string DetermineExpiredAt(Models.CheckoutConfiguration checkoutConfiguration)
        {
            var cetZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            var cetDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cetZone);

            var expiresAt = cetDateTime.AddDays(checkoutConfiguration.OrderExpiresInDays <= 0 ? 
                30 : 
                checkoutConfiguration.OrderExpiresInDays);

            return expiresAt.ToString("yyyy-MM-dd");
        }
    }
}
