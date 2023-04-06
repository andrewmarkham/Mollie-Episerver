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
    [ServiceConfiguration(typeof(IMolliePaymentMethodFilter))]
    public class MolliePaymentMethodFilter : IMolliePaymentMethodFilter
    {
        private readonly IPaymentMethodsSettingsService _paymentMethodsSettingsService;
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;

        public MolliePaymentMethodFilter(
            IPaymentMethodsSettingsService paymentMethodsSettingsService,
            ICheckoutConfigurationLoader checkoutConfigurationLoader)
        {
            _paymentMethodsSettingsService = paymentMethodsSettingsService;
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
        }

        public IEnumerable<PaymentMethodResponse> Filter(
            IEnumerable<PaymentMethodResponse> input,
            string languageId,
            string countryCode,
            string marketId)
        {
            var config = _checkoutConfigurationLoader.GetConfiguration(languageId);
            var settings = _paymentMethodsSettingsService.GetSettings(config.PaymentMethodId);

            var disabled = string.IsNullOrWhiteSpace(settings.DisabledPaymentMethods)
                ? new EditableList<MolliePaymentMethod>()
                : JsonConvert.DeserializeObject<List<MolliePaymentMethod>>(settings.DisabledPaymentMethods);

            disabled = disabled.Where(pm => pm.CountryCode == countryCode && pm.MarketId == marketId && pm.OrderApi == config.UseOrdersApi).ToList();

            return input.Where(pm => disabled.All(x => pm.Id != x.Id));
        }
    }
}
