using EPiServer.Business.Commerce;
using EPiServer.Commerce.Marketing;
using EPiServer.Commerce.Order;
using EPiServer.Framework.Localization.XmlResources;
using EPiServer.Web;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mollie.Checkout
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMolliePayments(this IServiceCollection services)
        {

            // Add core services
           // services.AddSingleton<IRegisterDisplayOptions, DisplayOptionRegistrar>();
           
           services.Intercept<IShipmentProcessor>(
                (locator, defaultShipmentProcessor) => new MollieShipmentProcessor(defaultShipmentProcessor));

            return services;
        }









    }
}
