using EPiServer.ServiceLocation;
using Mollie.Checkout.ProcessCheckout.Helpers.Interfaces;
using Mollie.Checkout.ProcessCheckout.Interfaces;
using Mollie.Checkout.Services;

namespace Mollie.Checkout.ProcessCheckout.Helpers
{
    [ServiceConfiguration(typeof(IProcessCheckoutFactory))]
    public class ProcessCheckoutFactory : IProcessCheckoutFactory
    {
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;

        public ProcessCheckoutFactory(
            ICheckoutConfigurationLoader checkoutConfigurationLoader)
        {
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
        }

        public IProcessCheckout GetInstance(string languageId)
        {
            var checkoutConfiguration = _checkoutConfigurationLoader.GetConfiguration(languageId);

            if (checkoutConfiguration.UseOrdersApi)
            {
                return new ProcessOrderCheckout();
            }

            return new ProcessPaymentCheckout();
        }
    }
}
