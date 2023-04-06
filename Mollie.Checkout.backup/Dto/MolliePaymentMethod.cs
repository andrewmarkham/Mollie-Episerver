
namespace Mollie.Checkout.Dto
{
    public class MolliePaymentMethod
    {
        public string Id { get; set; }
        public string MarketId { get; set; }
        public string CountryCode { get; set; }
        public bool OrderApi { get; set; }
        public string Description { get; set; }
        public int Rank { get; set; }
    }
}