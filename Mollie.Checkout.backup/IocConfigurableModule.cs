using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using Mollie.Checkout.Services;
using System.Net.Http;

namespace Mollie.Checkout
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class IocConfigurableModule : IConfigurableModule
    {
        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            var httpClient = new HttpClient();

            var versionString = AssemblyVersionUtils.CreateVersionString();

            httpClient.DefaultRequestHeaders.Add("user-agent", versionString);

            context.Services.AddSingleton(httpClient);
        }

        public void Initialize(InitializationEngine context) { }

        public void Uninitialize(InitializationEngine context) { }
    }
}