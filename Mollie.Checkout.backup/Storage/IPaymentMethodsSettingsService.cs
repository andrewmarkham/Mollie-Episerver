using System;
using Mollie.Checkout.Storage.Models;

namespace Mollie.Checkout.Storage
{
    public interface IPaymentMethodsSettingsService
    {
        PaymentMethodsSettings GetSettings(Guid paymentMethodsId);

        void SaveSettings(PaymentMethodsSettings aymentMethodsSettings);
    }
}
