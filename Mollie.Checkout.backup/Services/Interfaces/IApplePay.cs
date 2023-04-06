using System.Threading.Tasks;
using Mediachase.Commerce;

namespace Mollie.Checkout.Services.Interfaces
{
    public interface IApplePay
    {
        Task<bool> ApplePayDirectIntegrationActiveAsync(
            IMarket market,
            string countryCode,
            decimal price);
    }
}
