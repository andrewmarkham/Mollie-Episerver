using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter;
using EPiServer.Framework.Localization;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mollie.Api.Client;
using Mollie.Api.Client.Abstract;
using Mollie.Api.Models;
using Mollie.Api.Models.PaymentMethod;
using Mollie.Checkout.MollieApi;
using Mollie.Checkout.Helpers;
using Currency = Mediachase.Commerce.Currency;

namespace Mollie.Checkout.Services
{
    [ServiceConfiguration(typeof(IPaymentMethodsService))]
    public class PaymentMethodService : IPaymentMethodsService
    {
        private readonly LocalizationService _localizationService;
        private readonly IMolliePaymentMethodFilter _molliePaymentMethodFilter;
        private readonly IMolliePaymentMethodSorter _molliePaymentMethodSorter;
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly HttpClient _httpClient;
        
        public PaymentMethodService(
            LocalizationService localizationService,
            IMolliePaymentMethodFilter molliePaymentMethodFilter,
            IMolliePaymentMethodSorter molliePaymentMethodSorter,
            ICheckoutConfigurationLoader checkoutConfigurationLoader,
            HttpClient httpClient)
        {
            _localizationService = localizationService;
            _molliePaymentMethodFilter = molliePaymentMethodFilter;
            _molliePaymentMethodSorter = molliePaymentMethodSorter;
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
            _httpClient = httpClient;
        }

        public async Task<List<Models.PaymentMethod>> LoadMethods(
            string languageId)
        {
            // Load configuration
            var config = _checkoutConfigurationLoader.GetConfiguration(languageId);

            string locale = LanguageUtils.GetLocale(languageId);

            // Get Payment Methods
            IPaymentMethodClient client = new PaymentMethodClient(config.ApiKey, _httpClient);

            var resource = config.UseOrdersApi
                ? Api.Models.Payment.Resource.Orders
                : Api.Models.Payment.Resource.Payments;

            var result = await client.GetPaymentMethodListAsync(locale: locale, resource: resource, includeIssuers: true);

            return result.Items.Select(MapToModel).ToList();
        }

        public async Task<List<Models.PaymentMethod>> LoadMethods(
            string marketId,
            string languageId,
            string countryCode)
        { 
            // Load configuration
            var config = _checkoutConfigurationLoader.GetConfiguration(languageId);

            string locale = LanguageUtils.GetLocale(languageId);

            // Get Payment Methods
            IPaymentMethodClient client = new PaymentMethodClient(config.ApiKey, _httpClient);

            var resource = config.UseOrdersApi
                ? Api.Models.Payment.Resource.Orders
                : Api.Models.Payment.Resource.Payments;

            var result = await client.GetPaymentMethodListAsync(locale: locale, resource: resource, includeIssuers: true);

            var items = _molliePaymentMethodFilter.Filter(
                result.Items, 
                languageId,
                countryCode,
                marketId);

            items = _molliePaymentMethodSorter.Sort(
                items, 
                languageId,
                countryCode,
                marketId);

            return items.Select(MapToModel).ToList();
        }

        public async Task<bool> PaymentMethodActiveAsync(
            string paymentMethodId,
            string marketId,
            string languageId,
            Money cartTotal,
            string countryCode)
        {
            var config = _checkoutConfigurationLoader.GetConfiguration(languageId);

            var paymentMethods = await LoadMethods(
                languageId,
                cartTotal.Currency,
                cartTotal.Amount,
                countryCode,
                config.ApiKey,
                config.UseOrdersApi,
                true);

            if (paymentMethods.All(x => x.Id != paymentMethodId))
            {
                return false;
            }

            var items = _molliePaymentMethodFilter.Filter(
                paymentMethods,
                languageId,
                countryCode,
                marketId);

            return items.Any(x => x.Id == paymentMethodId);
        }

        public async Task<List<Models.PaymentMethod>> LoadMethods(
            string marketId,
            string languageId, 
            Money cartTotal, 
            string countryCode)
        {
            // Load configuration
            var config = _checkoutConfigurationLoader.GetConfiguration(languageId);

            var paymentMethods = await LoadMethods(
                languageId,
                cartTotal.Currency,
                cartTotal.Amount,
                countryCode,
                config.ApiKey,
                config.UseOrdersApi,
                true);

            var items = _molliePaymentMethodFilter.Filter(
                paymentMethods, 
                languageId,
                countryCode,
                marketId);

            items = _molliePaymentMethodSorter.Sort(
                items, 
                languageId,
                countryCode,
                marketId);

            return items.Select(MapToModel).ToList();
        }

        public async Task<List<PaymentMethodResponse>> LoadMethods(
            string languageId,
            Currency currency,
            decimal amount,
            string countryCode,
            string apiKey, 
            bool useOrderApi,
            bool includeIssuers)
        {
            // Get Payment Methods
            var client = new MolliePaymentMethodClient(apiKey, _httpClient);

            var resource = useOrderApi
                ? Api.Models.Payment.Resource.Orders
                : Api.Models.Payment.Resource.Payments;

            var billingCountry = countryCode?.Length == 3 ? CountryCodeMapper.MapToTwoLetterIsoRegion(countryCode) : countryCode;
            var locale = LanguageUtils.GetLocale(languageId, countryCode);

            var result = await client.GetPaymentMethodListAsync(
                locale: locale, 
                resource: resource, 
                amount: currency == Currency.Empty ? null : new Amount(currency.CurrencyCode, amount), 
                includeIssuers: includeIssuers, 
                billingCountry: billingCountry);

            return result.Items;
        }

        public IEnumerable<KeyValuePair<string, string>> GetCurrencyValidationIssues(
            string languageId,
            string countryCode,
            string apiKey,
            bool useOrdersApi,
            IMarket market)
        {
            var enabledPaymentMethods = AsyncHelper.RunSync(() => LoadMethods(
                market.MarketId.Value,
                languageId,
                countryCode));

            foreach (var currency in market.Currencies)
            {
                foreach (var enabledPaymentMethod in enabledPaymentMethods)
                {
                    List<PaymentMethodResponse> currencyPaymentMethods;

                    try
                    {
                        currencyPaymentMethods = AsyncHelper.RunSync(() => LoadMethods(
                            languageId,
                            currency,
                            1000,
                            countryCode,
                            apiKey,
                            useOrdersApi,
                            false));
                    }
                    catch
                    {
                        currencyPaymentMethods = new EditableList<PaymentMethodResponse>();
                    }

                    if (currencyPaymentMethods.All(cpm => cpm.Id != enabledPaymentMethod.Id))
                    {
                        yield return new KeyValuePair<string, string>(currency, enabledPaymentMethod.Description);
                    }
                }
            }
        }

        private Models.PaymentMethod MapToModel(PaymentMethodResponse response)
        {
            var methodModel = new Models.PaymentMethod
            {
                Id = response.Id,
                Description = _localizationService.GetString($"/mollie/paymentmethods/{response.Id}", response.Description),
                ImageSize1X = response.Image?.Size1x,
                ImageSize2X = response.Image?.Size2x,
                ImageSvg = response.Image?.Svg,
                Issuers = response.Issuers != null ?
                response.Issuers :
                null
            };
            
            if (response.MinimumAmount != null)
            {
                methodModel.MinimumAmount = new Money(response.MinimumAmount, response.MinimumAmount.Currency);
            }

            if (response.MaximumAmount != null)
            {
                methodModel.MaximumAmount = new Money(response.MaximumAmount, response.MaximumAmount.Currency);
            }

            return methodModel;
        }
    }
}
