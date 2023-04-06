using System;
using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace Mollie.Checkout.Storage.Models
{
    [EPiServerDataStore(AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
    public class PaymentMethodsSettings
    {
        public Identity Id { get; set; } //Is paymentMethodsId
        public string EnabledPaymentMethods { get; set; }
        public string DisabledPaymentMethods { get; set; }
    }
}
