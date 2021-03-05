﻿using System.Collections.Generic;
using System.Linq;
using Castle.Components.DictionaryAdapter;
using EPiServer.ServiceLocation;
using Mollie.Api.Models.PaymentMethod;
using Mollie.Checkout.Dto;
using Mollie.Checkout.Services;
using Newtonsoft.Json;

namespace Mollie.Checkout.Helpers
{
    [ServiceConfiguration(typeof(IMolliePaymentMethodFilter))]
    public class MolliePaymentMethodFilter : IMolliePaymentMethodFilter
    {
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;

        public MolliePaymentMethodFilter(
            ICheckoutConfigurationLoader checkoutConfigurationLoader)
        {
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
        }

        public IEnumerable<PaymentMethodResponse> Filter(
            IEnumerable<PaymentMethodResponse> input,
            string languageId)
        {
            var checkoutConfiguration = _checkoutConfigurationLoader.GetConfiguration(languageId);

            var disabled = string.IsNullOrWhiteSpace(checkoutConfiguration.DisabledMolliePaymentMethods)
                ? new EditableList<MolliePaymentMethod>()
                : JsonConvert.DeserializeObject<List<MolliePaymentMethod>>(checkoutConfiguration.DisabledMolliePaymentMethods);

            var locale = LanguageUtils.GetLocale(languageId);
            disabled = disabled.Where(pm => pm.Locale == locale).ToList();

            return input.Where(pm => disabled.All(x => pm.Id != x.Id));
        }
    }
}
