using System;
using System.Collections;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Storage;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using FakeItEasy;
using Mediachase.Commerce;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mollie.Api.Models.Payment.Request;
using Mollie.Api.Models.Payment.Response;
using Mollie.Api.Models.Url;
using Mollie.Checkout.Models;
using Mollie.Checkout.MollieClients;
using Mollie.Checkout.ProcessCheckout;
using Mollie.Checkout.Services;
using Mollie.Checkout.Services.Interfaces;

namespace Mollie.Checkout.Tests.ProcessCheckout
{
    [TestClass]
    public class ProcessPaymentCheckoutTests
    {
        private ILogger _logger;
        private ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private IPaymentDescriptionGenerator _paymentDescriptionGenerator;
        private ICheckoutMetaDataFactory _checkoutMetaDataFactory;
        private IOrderRepository _orderRepository;
        private ServiceAccessor<HttpContextBase> _httpContextAccessor;
        private HttpClient _httpClient;
        private ProcessPaymentCheckout _processCheckout;
        private IMolliePaymentClient _molliePaymentClient;
        private IOrderNoteHelper _orderNoteHelper;

        private ICart _cart;
        private IPayment _payment;

        private const decimal Amount = 10;
        private const string CurrencyCode = "EUR";
        private const string PaymentDescription = "Payment Description";
        private const string RedirectUrl = "https://www.mollie.com/";
        private const string WebShopUrl = "https://www.webshop.com";
        private const string OrderNumber = "PO0001";
        private const string Language = "en";
        private const string PaymentResponseId = nameof(PaymentResponseId);

        [ExpectedException(typeof(ApplicationException))]
        [TestMethod]
        public void When_Process_Payment_Invoked_And_Redirect_Api_Key_Null_Must_Throw_Expected_Exception()
        {
            SetupPayment();
            SetupCart();

            var checkoutConfiguration = new CheckoutConfiguration
            {
                RedirectUrl = nameof(CheckoutConfiguration.RedirectUrl),
                ApiKey = null
            };

            A.CallTo(() => _checkoutConfigurationLoader.GetConfiguration(A<string>._)).Returns(checkoutConfiguration);

            var paymentProcessingResult = _processCheckout.Process(_cart, _payment);
            Assert.IsTrue(paymentProcessingResult.IsSuccessful);
        }

        [ExpectedException(typeof(ApplicationException))]
        [TestMethod]
        public void When_Process_Payment_Invoked_And_Redirect_Configuration_Null_Must_Throw_Expected_Exception()
        {
            SetupPayment();
            SetupCart();

            var checkoutConfiguration = new CheckoutConfiguration
            {
                RedirectUrl = null,
                ApiKey = nameof(CheckoutConfiguration.ApiKey)
            };

            A.CallTo(() => _checkoutConfigurationLoader.GetConfiguration(A<string>._)).Returns(checkoutConfiguration);

            var paymentProcessingResult = _processCheckout.Process(_cart, _payment);
            Assert.IsTrue(paymentProcessingResult.IsSuccessful);
        }

        [ExpectedException(typeof(CultureNotFoundException))]
        [TestMethod]
        public void When_Process_Payment_Invoked_And_Payment_Language_Is_Null_Must_Throw_Expected_Exception()
        {
            SetupConfiguration();
            SetupCart();

            var paymentProperties = A.Fake<IExtendedProperties>();
            A.CallTo(() => paymentProperties.Properties).Returns(new Hashtable
            {
                { Constants.OtherPaymentFields.LanguageId, null }
            });
            A.CallTo(() => _payment.Properties).Returns(paymentProperties.Properties);

            A.CallTo(() => _payment.Amount).Returns(10);

            _ = _processCheckout.Process(_cart, _payment);
        }

        [TestMethod]
        public void When_Process_Payment_Invoked_And_All_Is_Ok_Must_Return_Successful_Result()
        {
            SetupConfiguration();
            SetupPayment();
            SetupCart();

            var paymentProcessingResult = _processCheckout.Process(_cart, _payment);
            Assert.IsTrue(paymentProcessingResult.IsSuccessful);
        }

        [TestMethod]
        public void When_Process_Payment_Invoked_And_All_Is_Ok_Must_Call_Mollie_Payment_Client()
        {
            SetupConfiguration();
            SetupPayment();
            SetupCart();

            _ = _processCheckout.Process(_cart, _payment);

            A.CallTo(() =>
                    _molliePaymentClient.CreatePaymentAsync(
                        A<PaymentRequest>.That.Matches(x => x.Amount.Value == Amount.ToString("0.00", CultureInfo.InvariantCulture) && x.Amount.Currency == CurrencyCode),
                        A<string>._,
                        A<HttpClient>._))
                .MustHaveHappened();

            A.CallTo(() =>
                    _molliePaymentClient.CreatePaymentAsync(
                        A<PaymentRequest>.That.Matches(x => x.Description == PaymentDescription),
                        A<string>._,
                        A<HttpClient>._))
                .MustHaveHappened();

            var redirectUrl = RedirectUrl + $"?orderNumber={OrderNumber}";
            A.CallTo(() =>
                    _molliePaymentClient.CreatePaymentAsync(
                        A<PaymentRequest>.That.Matches(x => x.RedirectUrl == redirectUrl),
                        A<string>._,
                        A<HttpClient>._))
                .MustHaveHappened();

            var urlBuilder = new UriBuilder(WebShopUrl)
            {
                Path = $"{Constants.Webhooks.MolliePaymentsWebhookUrl}/{Language}"
            };

            var webhookUrl = urlBuilder.ToString();
            A.CallTo(() =>
                    _molliePaymentClient.CreatePaymentAsync(
                        A<PaymentRequest>.That.Matches(x => x.WebhookUrl == webhookUrl),
                        A<string>._,
                        A<HttpClient>._))
                .MustHaveHappened();

            A.CallTo(() =>
                    _molliePaymentClient.CreatePaymentAsync(
                        A<PaymentRequest>.That.Matches(x => x.Locale == new CultureInfo(Language).TextInfo.CultureName),
                        A<string>._,
                        A<HttpClient>._))
                .MustHaveHappened();
        }

        [TestMethod]
        public void When_Process_Payment_Invoked_And_All_Is_Ok_Must_Add_Note_To_Order()
        {
            SetupConfiguration();
            SetupPayment();
            SetupCart();

            _ = _processCheckout.Process(_cart, _payment);

            A.CallTo(() => _orderNoteHelper.AddNoteToOrder(A<IOrderGroup>._, A<string>._, A<string>._, A<Guid>._))
                .MustHaveHappened();
        }

        [TestMethod]
        public void When_Process_Payment_Invoked_And_All_Is_Ok_Must_Save_Cart()
        {
            SetupConfiguration();
            SetupPayment();
            SetupCart();

            _ = _processCheckout.Process(_cart, _payment);
            A.CallTo(() => _orderRepository.Save(A<ICart>._)).MustHaveHappened();
        }

        [TestMethod]
        public void When_Process_Payment_Invoked_And_All_Is_Ok_Must_Add_Mollie_Payment_Id_To_Payment()
        {
            SetupConfiguration();
            SetupPayment();
            SetupCart();

            _ = _processCheckout.Process(_cart, _payment);

            Assert.AreEqual(PaymentResponseId, _payment.Properties[Constants.OtherPaymentFields.MolliePaymentId]);
        }

        [TestInitialize]
        public void Setup()
        {
            _logger = A.Fake<ILogger>();
            _checkoutConfigurationLoader = A.Fake<ICheckoutConfigurationLoader>();
            _paymentDescriptionGenerator = A.Fake<IPaymentDescriptionGenerator>();
            _checkoutMetaDataFactory = A.Fake<ICheckoutMetaDataFactory>();
            _orderRepository = A.Fake<IOrderRepository>();
            _httpContextAccessor = A.Fake<ServiceAccessor<HttpContextBase>>();
            _httpClient = A.Fake<HttpClient>();
            _molliePaymentClient = A.Fake<IMolliePaymentClient>();
            _orderNoteHelper = A.Fake<IOrderNoteHelper>();

            var httpContext = new HttpContext(new HttpRequest(null, WebShopUrl, null), new HttpResponse(null));
            A.CallTo(() => _httpContextAccessor.Invoke()).Returns(new HttpContextWrapper(httpContext));

            A.CallTo(() => _paymentDescriptionGenerator.GetDescription(A<IOrderGroup>._, A<IPayment>._))
                .Returns(PaymentDescription);

            A.CallTo(() => _checkoutMetaDataFactory.Create(A<IOrderGroup>._, A<IPayment>._, A<CheckoutConfiguration>._))
                .Returns(new CheckoutMetaDataModel
                {
                    OrderGroupId = 1,
                    OrderNumber = nameof(CheckoutMetaDataModel.OrderNumber),
                    VersionStrings = nameof(CheckoutMetaDataModel.VersionStrings)
                });

            A.CallTo(() =>
                    _molliePaymentClient.CreatePaymentAsync(A<PaymentRequest>._, A<string>.Ignored, A<HttpClient>._))
                .Returns(Task.FromResult(new PaymentResponse
                {
                    Id = PaymentResponseId,
                    Links = new PaymentResponseLinks { Checkout = new UrlLink { Href = "https://www.mollie.com" } }
                }));

            _processCheckout = new ProcessPaymentCheckout(
                _logger,
                _checkoutConfigurationLoader,
                _paymentDescriptionGenerator,
                _checkoutMetaDataFactory,
                _orderRepository,
                _httpContextAccessor,
                _httpClient,
                _molliePaymentClient,
                _orderNoteHelper);

            _cart = A.Fake<ICart>();
            _payment = A.Fake<IPayment>();
        }

        private void SetupConfiguration()
        {
            var checkoutConfiguration = new CheckoutConfiguration
            {
                ApiKey = "e9150793-2705-47b8-8411-a2e02f9f68fd",
                RedirectUrl = RedirectUrl
            };

            A.CallTo(() => _checkoutConfigurationLoader.GetConfiguration(A<string>._)).Returns(checkoutConfiguration);
        }

        private void SetupCart()
        {
            A.CallTo(() => _cart.Currency).Returns(new Currency(CurrencyCode));

            var cartProperties = A.Fake<IExtendedProperties>();
            A.CallTo(() => cartProperties.Properties).Returns(new Hashtable
            {
                { "OrderNumber", OrderNumber }
            });
            A.CallTo(() => _cart.Properties).Returns(cartProperties.Properties);
        }

        private void SetupPayment()
        {
            var paymentProperties = A.Fake<IExtendedProperties>();
            A.CallTo(() => paymentProperties.Properties).Returns(new Hashtable
            {
                { Constants.OtherPaymentFields.LanguageId, Language }
            });
            A.CallTo(() => _payment.Properties).Returns(paymentProperties.Properties);

            A.CallTo(() => _payment.Amount).Returns(Amount);
        }
    }
}
