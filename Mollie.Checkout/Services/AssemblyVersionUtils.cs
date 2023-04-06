using System;
using System.IO;
using System.Reflection;

namespace Mollie.Checkout.Services
{
    public static class AssemblyVersionUtils
    {
        public static string CreateVersionString()
        {
            var mollieCheckoutVersion = GetAssemblyVersion("Mollie.Checkout.dll");
            var episerverVersion = GetAssemblyVersion("EPiServer.dll");
            var episerverCommerceVersion = GetAssemblyVersion("Mediachase.Commerce.dll");

            return $"MollieEpiserver/{mollieCheckoutVersion} EpiserverCommerce/{episerverCommerceVersion} Episerver/{episerverVersion}";
        }

        private static string GetAssemblyVersion(string asssembly)
        {
            if (string.IsNullOrWhiteSpace(asssembly))
            {
                throw new ArgumentNullException(nameof(asssembly));
            }

            var location = Assembly.GetExecutingAssembly().Location;
            var pathName = Path.GetDirectoryName(location) ?? string.Empty;
            var assemblyFolderUri = new Uri(pathName);

            AssemblyName assemblyName = AssemblyName.GetAssemblyName($"{assemblyFolderUri.LocalPath}\\{asssembly}");

            return assemblyName.Version.ToString();
        }
    }
}