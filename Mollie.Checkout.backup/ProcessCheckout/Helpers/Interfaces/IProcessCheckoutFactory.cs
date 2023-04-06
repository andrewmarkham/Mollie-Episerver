using Mollie.Checkout.ProcessCheckout.Interfaces;

namespace Mollie.Checkout.ProcessCheckout.Helpers.Interfaces
{
    public interface IProcessCheckoutFactory
    {
        IProcessCheckout GetInstance(string languageId);
    }
}
