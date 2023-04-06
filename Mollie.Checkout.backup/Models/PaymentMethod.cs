using Mediachase.Commerce;
using Mollie.Api.Models.Issuer;
using System.Collections.Generic;

namespace Mollie.Checkout.Models
{
    public class PaymentMethod
    {
        public string Id { get; set; }

        public string Description { get; set; }

        public Money MinimumAmount { get; set; }

        public Money MaximumAmount { get; set; }

        public string ImageSize1X { get; set; }

        public string ImageSize2X { get; set; }

        public string ImageSvg { get; set; }

        public IEnumerable<IssuerResponse> Issuers { get; set; }

    }
}
