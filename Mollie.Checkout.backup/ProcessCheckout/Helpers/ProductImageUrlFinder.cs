using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Mollie.Checkout.ProcessCheckout.Helpers.Interfaces;

namespace Mollie.Checkout.ProcessCheckout.Helpers
{
    [ServiceConfiguration(typeof(IProductImageUrlFinder))]
    public class ProductImageUrlFinder : IProductImageUrlFinder
    {
        private readonly UrlResolver _urlResolver;
        private readonly IContentLoader _contentLoader;

        public ProductImageUrlFinder(
            UrlResolver urlResolver,
            IContentLoader contentLoader)
        {
            _urlResolver = urlResolver;
            _contentLoader = contentLoader;
        }

        public string Find(EntryContentBase entry)
        {
            if (!(entry is IAssetContainer assetContainer))
            {
                return SiteDefinition.Current.SiteUrl.ToString();
            }

            var productImageUrl = assetContainer.CommerceMediaCollection.Select(media =>
            {
                if (!_contentLoader.TryGet<IContentMedia>(media.AssetLink, out var contentMedia))
                {
                    return new KeyValuePair<string, string>(string.Empty, string.Empty);
                }

                var type = "Image";
                var url = _urlResolver.GetUrl(media.AssetLink, null, new VirtualPathArguments { ContextMode = ContextMode.Default });
                if (contentMedia is IContentVideo)
                {
                    type = "Video";
                }

                return new KeyValuePair<string, string>(type, url);

            }).FirstOrDefault(m => m.Key == "Image");

            if (string.IsNullOrWhiteSpace(productImageUrl.Value))
            {
                return SiteDefinition.Current.SiteUrl.ToString();
            }

            var absoluteUrl = new Uri(SiteDefinition.Current.SiteUrl, productImageUrl.Value);

            return absoluteUrl.ToString();
        }
    }
}
