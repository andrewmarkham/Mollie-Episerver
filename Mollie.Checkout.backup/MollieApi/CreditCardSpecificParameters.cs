using Mollie.Api.Models.Order.Request.PaymentSpecificParameters;

namespace Mollie.Checkout.MollieApi
{
    public class CreditCardSpecificParameters : PaymentSpecificParameters
    {
        public string CardToken { get; set; }
    }
}
