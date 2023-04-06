using System;
using System.Collections;
using System.Net.Http;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Storage;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mollie.Api.Models.Refund;
using Mollie.Checkout.Helpers;
using Mollie.Checkout.MollieClients;
using Mollie.Checkout.ProcessRefund;
using Mollie.Checkout.Services;
using Mollie.Checkout.Services.Interfaces;

namespace Mollie.Checkout.Tests.ProcessRefund
{
    [TestClass]
    public class ProcessPaymentRefundTests
    {
        private IReturnOrderFormFinder _returnOrderFormFinder;
        private IMollieRefundClient _mollieRefundClient;
        private IOrderRepository _orderRepository;
        private ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private HttpClient _httpClient;
        private IOrderNoteHelper _orderNoteHelper;
        private ProcessPaymentRefund _processPaymentRefund;

        private IPayment _payment;
        private IPurchaseOrder _purchaseOrder;

        private const string Language = "en";
        private const string CurrencyCode = "EUR";
        private const string OrderNumber = "PO0001";
        private const decimal Amount = 10;
        private const string MolliePaymentId = nameof(MolliePaymentId);
        private const string ReturnComment = nameof(ReturnComment);

        [TestMethod]
        public void When_Process_Refund_Invoked_And_Order_Group_No_Purchase_Order_Must_Return_Not_Successful()
        {
            SetupPayment();

            var orderGroup = A.Fake<IOrderGroup>();

            var paymentProcessingResult = _processPaymentRefund.Process(orderGroup, _payment);
            Assert.IsFalse(paymentProcessingResult.IsSuccessful);
        }

        [TestMethod]
        public void When_Process_Refund_Invoked_And_All_Is_Ok_Must_Return_Successful_Result()
        {
            SetupPayment();
            SetupPurchaseOrder();

            var paymentProcessingResult = _processPaymentRefund.Process(_purchaseOrder, _payment);
            Assert.IsTrue(paymentProcessingResult.IsSuccessful);
        }

        [TestMethod]
        public void When_Process_Refund_Invoked_Must_Send_Return_Comment()
        {
            SetupPayment();
            SetupPurchaseOrder();

            _ = _processPaymentRefund.Process(_purchaseOrder, _payment);

            A.CallTo(() => _mollieRefundClient.CreateRefundAsync(
                A<string>.Ignored,
                A<RefundRequest>.That.Matches(x => x.Description == ReturnComment),
                A<string>.Ignored,
                A<HttpClient>.Ignored))
                .MustHaveHappened();
        }

        [TestMethod]
        public void When_Process_Refund_Invoked_Must_Send_Payment_Id()
        {
            SetupPayment();
            SetupPurchaseOrder();

            _ = _processPaymentRefund.Process(_purchaseOrder, _payment);

            A.CallTo(() => _mollieRefundClient.CreateRefundAsync(
                    A<string>.That.Matches(x => x == MolliePaymentId),
                    A<RefundRequest>.Ignored,
                    A<string>.Ignored,
                    A<HttpClient>.Ignored))
                .MustHaveHappened();
        }

        [TestMethod]
        public void When_Process_Refund_Invoked_Must_Send_Amount()
        {
            SetupPayment();
            SetupPurchaseOrder();

            _ = _processPaymentRefund.Process(_purchaseOrder, _payment);

            A.CallTo(() => _mollieRefundClient.CreateRefundAsync(
                    A<string>.Ignored,
                    A<RefundRequest>.That.Matches(x => x.Amount == Amount),
                    A<string>.Ignored,
                    A<HttpClient>.Ignored))
                .MustHaveHappened();
        }

        [TestMethod]
        public void When_Process_Refund_Invoked_Must_Add_Order_Note()
        {
            SetupPayment();
            SetupPurchaseOrder();

            _ = _processPaymentRefund.Process(_purchaseOrder, _payment);

            A.CallTo(() => _orderNoteHelper.AddNoteToOrder(
                A<IOrderGroup>.Ignored,
                A<string>.Ignored,
                A<string>.Ignored,
                A<Guid>.Ignored)).MustHaveHappened();

            A.CallTo(() => _orderRepository.Save(A<IOrderGroup>.Ignored))
                .MustHaveHappened();
        }

        [TestInitialize]
        public void Setup()
        {
            _returnOrderFormFinder = A.Fake<IReturnOrderFormFinder>();
            _mollieRefundClient = A.Fake<IMollieRefundClient>();
            _orderRepository = A.Fake<IOrderRepository>();
            _checkoutConfigurationLoader = A.Fake<ICheckoutConfigurationLoader>();
            _orderNoteHelper = A.Fake<IOrderNoteHelper>();
            _httpClient = A.Fake<HttpClient>();

            _processPaymentRefund = new ProcessPaymentRefund(
                _returnOrderFormFinder,
                _mollieRefundClient,
                _orderRepository,
                _httpClient,
                _checkoutConfigurationLoader,
                _orderNoteHelper);

            _purchaseOrder = A.Fake<IPurchaseOrder>();
            _payment = A.Fake<IPayment>();
        }

        private void SetupPurchaseOrder()
        {
            A.CallTo(() => _purchaseOrder.Currency).Returns(new Mediachase.Commerce.Currency(CurrencyCode));

            var cartProperties = A.Fake<IExtendedProperties>();
            A.CallTo(() => cartProperties.Properties).Returns(new Hashtable
            {
                { "OrderNumber", OrderNumber }
            });
            A.CallTo(() => _purchaseOrder.Properties).Returns(cartProperties.Properties);

            var returnOrderForm = A.Fake<IReturnOrderForm>();
            A.CallTo(() => returnOrderForm.ReturnComment).Returns(ReturnComment);

            A.CallTo(() => _returnOrderFormFinder.Find(A<IPurchaseOrder>.Ignored)).Returns(returnOrderForm);
        }

        private void SetupPayment()
        {
            var paymentProperties = A.Fake<IExtendedProperties>();
            A.CallTo(() => paymentProperties.Properties).Returns(new Hashtable
            {
                { Constants.OtherPaymentFields.LanguageId, Language },
                { Constants.OtherPaymentFields.MolliePaymentId, MolliePaymentId }
            });
            A.CallTo(() => _payment.Properties).Returns(paymentProperties.Properties);
            A.CallTo(() => _payment.Amount).Returns(Amount);
        }
    }
}
