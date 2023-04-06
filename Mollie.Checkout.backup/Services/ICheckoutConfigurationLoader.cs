using Mollie.Checkout.Models;

namespace Mollie.Checkout.Services
{
    public interface ICheckoutConfigurationLoader
    {
        CheckoutConfiguration GetConfiguration(string languageId);

        
    }
}
