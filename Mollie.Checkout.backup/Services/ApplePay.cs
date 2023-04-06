using System.Threading.Tasks;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mollie.Checkout.Services.Interfaces;

namespace Mollie.Checkout.Services
{
    [ServiceConfiguration(typeof(IApplePay))]
    public class ApplePay : IApplePay
    {
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly IPaymentMethodsService _paymentMethodsService;

        public ApplePay(
            ICheckoutConfigurationLoader checkoutConfigurationLoader,
            IPaymentMethodsService paymentMethodsService)
        {
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
            _paymentMethodsService = paymentMethodsService;
        }

        public async Task<bool> ApplePayDirectIntegrationActiveAsync(
            IMarket market,
            string countryCode,
            decimal price)
        {
            var language = market.DefaultLanguage;
            var marketId = market.MarketId.Value;

            var languageId = language.Name;
            var currency = market.DefaultCurrency;
            var money = new Money(price, currency);

            if (!_checkoutConfigurationLoader.GetConfiguration(languageId).UseApplePayDirectIntegration)
            {
                return false;
            }

            const string applePayPaymentMethodId = "applepay";

            return await  _paymentMethodsService
                .PaymentMethodActiveAsync(
                    applePayPaymentMethodId,
                    marketId,
                    languageId,
                    money,
                    countryCode);
        }
    }
}
