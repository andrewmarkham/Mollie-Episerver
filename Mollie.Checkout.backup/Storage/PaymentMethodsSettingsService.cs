using System;
using System.Linq;
using EPiServer.Data.Dynamic;
using EPiServer.ServiceLocation;
using Mollie.Checkout.Storage.Models;

namespace Mollie.Checkout.Storage
{
    [ServiceConfiguration(typeof(IPaymentMethodsSettingsService))]
    public class PaymentMethodsSettingsService : IPaymentMethodsSettingsService
    {
        public PaymentMethodsSettings GetSettings(Guid paymentMethodsId)
        {
            var store = typeof(PaymentMethodsSettings).GetOrCreateStore();

            var paymentMethodsSettings = store.Items<PaymentMethodsSettings>()
                .FirstOrDefault(viewData => viewData.Id.ExternalId == paymentMethodsId);

            return paymentMethodsSettings ?? new PaymentMethodsSettings();
        }

        public void SaveSettings(PaymentMethodsSettings paymentMethodsSettings)
        {
            var store = typeof(PaymentMethodsSettings).GetOrCreateStore();
            store.Save(paymentMethodsSettings);
        }
    }
}
