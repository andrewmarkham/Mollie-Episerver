using System.Collections.Generic;
using Mollie.Api.Models.PaymentMethod;

namespace Mollie.Checkout.Helpers
{
    public interface IMolliePaymentMethodSorter
    {
        IEnumerable<PaymentMethodResponse> Sort(
            IEnumerable<PaymentMethodResponse> input,
            string languageId,
            string countryCode,
            string marketId);
    }
}
