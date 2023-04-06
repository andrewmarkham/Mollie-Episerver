using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;

namespace Mollie.Checkout.Helpers
{
    [ServiceConfiguration(typeof(ICurrentCustomerContactGetter))]
    public class CurrentCustomerContactGetter : ICurrentCustomerContactGetter
    {
        private readonly CustomerContext _customerContext;

        public CurrentCustomerContactGetter()
        {
            _customerContext = CustomerContext.Current;
        }

        public CustomerContact Get()
        {
            return _customerContext.CurrentContact;
        }
    }
}
