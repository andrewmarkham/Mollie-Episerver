using EPiServer.Commerce.Order;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

namespace Mollie.Checkout.ProcessShipment
{
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class ShipmentProcessorInterceptor : IConfigurableModule
    {
        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Services.Intercept<IShipmentProcessor>(
                (locator, defaultShipmentProcessor) => new MollieShipmentProcessor(defaultShipmentProcessor));
        }
        public void Initialize(InitializationEngine context) { }
        public void Uninitialize(InitializationEngine context) { }
    }
}
