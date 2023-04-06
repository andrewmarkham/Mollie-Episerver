using System;
using System.Linq;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Mollie.Checkout.ProcessCheckout.Helpers.Interfaces;

namespace Mollie.Checkout.ProcessCheckout.Helpers
{
    [ServiceConfiguration(typeof(IProductUrlGetter))]
    public class ProductUrlGetter : IProductUrlGetter
    {
        private readonly IRelationRepository _relationRepository;
        private readonly UrlResolver _urlResolver;

        public ProductUrlGetter(
            IRelationRepository relationRepository,
            UrlResolver urlResolver)
        {
            _relationRepository = relationRepository;
            _urlResolver = urlResolver;
        }

        public string Get(EntryContentBase entry)
        {
            var productLink = entry is VariationContent
                ? entry.GetParentProducts(_relationRepository).FirstOrDefault()
                : entry.ContentLink;

            if (productLink == null)
            {
                return string.Empty;
            }

            var urlBuilder = new UrlBuilder(_urlResolver.GetUrl(productLink));

            if (entry.Code != null && entry is VariationContent)
            {
                urlBuilder.QueryCollection.Add("variationCode", entry.Code);
            }

            var absoluteUrl = new Uri(SiteDefinition.Current.SiteUrl, urlBuilder.ToString());

            return absoluteUrl.ToString();
        }
    }
}
