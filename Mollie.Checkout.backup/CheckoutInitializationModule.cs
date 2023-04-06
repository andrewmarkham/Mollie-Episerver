using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Managers;
using Mediachase.MetaDataPlus.Configurator;
using System;
using System.Linq;

namespace Mollie.Checkout
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
    public class CheckoutInitializationModule : IInitializableModule
    {
        private readonly Injected<IMarketService> _marketService;

        public void Initialize(InitializationEngine context)
        { 
            InitializeOtherPaymentMetaClass();
            InitializeCartMetaClass();
            InitializePurchaseOrderMetaClass();
            InitializePaymentLinkMollie();

            AddMollieCheckoutPaymentMethod();
        }

        public void Uninitialize(InitializationEngine context)
        {
            // Do nothing
        }

        private void AddMollieCheckoutPaymentMethod()
        {
            var allMarkets = _marketService.Service.GetAllMarkets().Where(x => x.IsEnabled).ToList();
            foreach (var language in allMarkets.SelectMany(x => x.Languages).Distinct())
            {
                var paymentMethodDto = PaymentManager.GetPaymentMethods(language.TwoLetterISOLanguageName, true);
                
                if (!paymentMethodDto.PaymentMethod.Any(pm => pm.SystemKeyword.Equals(Constants.MollieCheckoutSystemKeyword)))
                {
                    var row = paymentMethodDto.PaymentMethod.AddPaymentMethodRow(Guid.NewGuid(), Constants.MollieCheckoutMethodName, 
                        Constants.MollieCheckoutMethodName, language.TwoLetterISOLanguageName, Constants.MollieCheckoutSystemKeyword,
                        false, false, $"{typeof(MollieCheckoutGateway)}, {typeof(MollieCheckoutGateway).Assembly.GetName().Name}",
                        $"{typeof(OtherPayment)}, {typeof(OtherPayment).Assembly.GetName().Name}", false, 0, DateTime.Now, DateTime.Now);

                    var paymentMethod = new PaymentMethod(row);
                    paymentMethod.MarketId.AddRange(allMarkets.Where(x => x.IsEnabled && x.Languages.Contains(language)).Select(x => x.MarketId));
                    paymentMethod.SaveChanges();
                }
            }
        }

        private void InitializeOtherPaymentMetaClass()
        {
            var otherPaymentMetaClass = OrderContext.Current.OtherPaymentMetaClass;
            var metaDataContext = OrderContext.MetaDataContext;

            var languageIdField = MetaField.Load(metaDataContext, Constants.OtherPaymentFields.LanguageId);

            if (languageIdField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.OtherPaymentFields.LanguageId,
                    Constants.OtherPaymentFields.LanguageId, string.Empty, MetaDataType.ShortString, 10, true, false, false, false);
                otherPaymentMetaClass.AddField(metaField);
            }

            var molliePaymentIdField = MetaField.Load(metaDataContext, Constants.OtherPaymentFields.MolliePaymentId);

            if (molliePaymentIdField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.OtherPaymentFields.MolliePaymentId,
                    Constants.OtherPaymentFields.MolliePaymentId, string.Empty, MetaDataType.ShortString, 25, true, false, false, false);
                otherPaymentMetaClass.AddField(metaField);
            }

            var molliePaymentStatusField = MetaField.Load(metaDataContext, Constants.OtherPaymentFields.MolliePaymentStatus);

            if (molliePaymentStatusField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.OtherPaymentFields.MolliePaymentStatus,
                    Constants.OtherPaymentFields.MolliePaymentStatus, string.Empty, MetaDataType.ShortString, 25, true, false, false, false);
                otherPaymentMetaClass.AddField(metaField);
            }

            var molliePaymentMethodField = MetaField.Load(metaDataContext, Constants.OtherPaymentFields.MolliePaymentMethod);
            if (molliePaymentMethodField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.OtherPaymentFields.MolliePaymentMethod,
                    Constants.OtherPaymentFields.MolliePaymentMethod, string.Empty, MetaDataType.ShortString, 25, true, false, false, false);
                otherPaymentMetaClass.AddField(metaField);
            }

            var molliePaymentResultField = MetaField.Load(metaDataContext, Constants.OtherPaymentFields.MolliePaymentFullResult);
            if (molliePaymentResultField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.OtherPaymentFields.MolliePaymentFullResult,
                    Constants.OtherPaymentFields.MolliePaymentFullResult, string.Empty, MetaDataType.LongString, int.MaxValue, true, false, false, false);
                otherPaymentMetaClass.AddField(metaField);
            }

            var mollieIssuerPaymentField = MetaField.Load(metaDataContext, Constants.OtherPaymentFields.MollieIssuer);
            if (mollieIssuerPaymentField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.OtherPaymentFields.MollieIssuer,
                    Constants.OtherPaymentFields.MollieIssuer, string.Empty, MetaDataType.ShortString, 50, true, false, false, false);
                otherPaymentMetaClass.AddField(metaField);
            }

            var mollieTokenField = MetaField.Load(metaDataContext, Constants.OtherPaymentFields.MollieToken);
            if (mollieTokenField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.OtherPaymentFields.MollieToken,
                   Constants.OtherPaymentFields.MollieToken, string.Empty, MetaDataType.ShortString, 50, true, false, false, false);
                otherPaymentMetaClass.AddField(metaField);
            }
        }

        private void InitializeCartMetaClass()
        {
            var shoppingCartMetaClass = OrderContext.Current.ShoppingCartMetaClass;
            var metaDataContext = OrderContext.MetaDataContext;


            var orderIdMollieField = MetaField.Load(metaDataContext, Constants.MollieOrder.OrderIdMollie);
            if (orderIdMollieField == null)
            {
                // Create
                orderIdMollieField = MetaField.Create(metaDataContext, string.Empty, Constants.MollieOrder.OrderIdMollie,
                    Constants.MollieOrder.OrderIdMollie, string.Empty, MetaDataType.ShortString, 50, true, false, false, false);
            }

            if (!shoppingCartMetaClass.MetaFields.Any(field => field.Name == Constants.MollieOrder.OrderIdMollie))
            {
                // Add
                shoppingCartMetaClass.AddField(orderIdMollieField);
            }


            var mollieOrderStatusField = MetaField.Load(metaDataContext, Constants.Cart.MollieOrderStatusField);
            if (mollieOrderStatusField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.Cart.MollieOrderStatusField,
                    Constants.Cart.MollieOrderStatusField, string.Empty, MetaDataType.ShortString, 25, true, false, false, false);

                shoppingCartMetaClass.AddField(metaField);
            }
        }

        private void InitializePurchaseOrderMetaClass()
        {
            var purchaseOrderMetaClass = OrderContext.Current.PurchaseOrderMetaClass;
            var metaDataContext = OrderContext.MetaDataContext;


            var orderIdMollieField = MetaField.Load(metaDataContext, Constants.MollieOrder.OrderIdMollie);
            if (orderIdMollieField == null)
            {
                // Create
                orderIdMollieField = MetaField.Create(metaDataContext, string.Empty, Constants.MollieOrder.OrderIdMollie,
                    Constants.MollieOrder.OrderIdMollie, string.Empty, MetaDataType.ShortString, 50, true, false, false, false);
            }

            if (!purchaseOrderMetaClass.MetaFields.Any(field => field.Name == Constants.MollieOrder.OrderIdMollie))
            {
                // Add
                purchaseOrderMetaClass.AddField(orderIdMollieField);
            }

            var languageIdField = MetaField.Load(metaDataContext, Constants.MollieOrder.LanguageId);
            if (languageIdField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.MollieOrder.LanguageId,
                    Constants.MollieOrder.LanguageId, string.Empty, MetaDataType.ShortString, 10, true, false, false, false);
                purchaseOrderMetaClass.AddField(metaField);
            }

            var mollieOrderStatusField = MetaField.Load(metaDataContext, Constants.Cart.MollieOrderStatusField);
            if (mollieOrderStatusField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.Cart.MollieOrderStatusField,
                    Constants.Cart.MollieOrderStatusField, string.Empty, MetaDataType.ShortString, 25, true, false, false, false);

                purchaseOrderMetaClass.AddField(metaField);
            }
        }

        private void InitializePaymentLinkMollie()
        {
            var purchaseOrderMetaClass = OrderContext.Current.PurchaseOrderMetaClass;
            var shoppingCartMetaClass = OrderContext.Current.ShoppingCartMetaClass;
            var metaDataContext = OrderContext.MetaDataContext;

            var paymentLinkMollieField = MetaField.Load(metaDataContext, Constants.PaymentLinkMollie);
            if (paymentLinkMollieField != null)
            {
                return;
            }

            var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.PaymentLinkMollie,
                Constants.PaymentLinkMollie, string.Empty, MetaDataType.LongString, 500, true, false, false, false);

            purchaseOrderMetaClass.AddField(metaField);
            shoppingCartMetaClass.AddField(metaField);
        }
    }
}