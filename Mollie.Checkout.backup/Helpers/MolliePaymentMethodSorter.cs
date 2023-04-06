using System.Collections.Generic;
using System.Linq;
using Castle.Components.DictionaryAdapter;
using EPiServer.ServiceLocation;
using Mollie.Api.Models.PaymentMethod;
using Mollie.Checkout.Dto;
using Mollie.Checkout.Services;
using Mollie.Checkout.Storage;
using Newtonsoft.Json;

namespace Mollie.Checkout.Helpers
{
    [ServiceConfiguration(typeof(IMolliePaymentMethodSorter))]
    public class MolliePaymentMethodSorter : IMolliePaymentMethodSorter
    {
        private readonly IPaymentMethodsSettingsService _paymentMethodsSettingsService;
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;

        public MolliePaymentMethodSorter(
            IPaymentMethodsSettingsService paymentMethodsSettingsService,
            ICheckoutConfigurationLoader checkoutConfigurationLoader)
        {
            _paymentMethodsSettingsService = paymentMethodsSettingsService;
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
        }

        public IEnumerable<PaymentMethodResponse> Sort(
            IEnumerable<PaymentMethodResponse> input,
            string languageId,
            string countryCode,
            string marketId)
        {
            var config = _checkoutConfigurationLoader.GetConfiguration(languageId);
            var settings = _paymentMethodsSettingsService.GetSettings(config.PaymentMethodId);

            var enabled = string.IsNullOrWhiteSpace(settings.EnabledPaymentMethods)
                ? new EditableList<MolliePaymentMethod>()
                : JsonConvert.DeserializeObject<List<MolliePaymentMethod>>(settings.EnabledPaymentMethods);

            var enabledIds = enabled
                .Where(pm => pm.CountryCode == countryCode && pm.MarketId == marketId && pm.OrderApi == config.UseOrdersApi)
                .Select(pm => pm.Id)
                .ToList();

            return input.OrderBy(pm => enabledIds.IndexOf(pm.Id));
        }
    }
}
