using Mediachase.Commerce.Customers;

namespace Mollie.Checkout.Helpers
{
    public interface ICurrentCustomerContactGetter
    {
        CustomerContact Get();
    }
}
