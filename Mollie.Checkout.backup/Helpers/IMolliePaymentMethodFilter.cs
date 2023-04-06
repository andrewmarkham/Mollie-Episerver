using System.Collections.Generic;
using Mollie.Api.Models.PaymentMethod;

namespace Mollie.Checkout.Helpers
{
    public interface IMolliePaymentMethodFilter
    {
        IEnumerable<PaymentMethodResponse> Filter(
            IEnumerable<PaymentMethodResponse> input,
            string languageId,
            string countryCode,
            string marketId);
    }
}
