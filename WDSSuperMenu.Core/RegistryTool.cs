using Microsoft.Win32;

namespace WDSSuperMenu.Core
{
    public class RegistryProductEntry
    {
        public string ProductName { get; set; }
        public string ProductIcon { get; set; }
        public string Version { get; set; }
        public RegistryProductEntry(string productName, string productIcon, string version)
        {
            ProductName = productName;
            ProductIcon = productIcon;
            Version = version;
        }
    }

    public class RegistryOptionsEntry
    {
        public string RegistryPath { get; set; }
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
        public RegistryOptionsEntry(string registryPath)
        {
            RegistryPath = registryPath;
        }
    }

    public static class RegistryTool
    {
        public static Dictionary<string, RegistryProductEntry> BuildRegistryIconCache()
        {
            var cache = new Dictionary<string, RegistryProductEntry>();
            try
            {
                using (RegistryKey productsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\Installer\Products"))
                {
                    if (productsKey == null)
                    {
                        Logger.LogToFile("Failed to open registry key HKLM\\SOFTWARE\\Classes\\Installer\\Products");
                        return cache;
                    }

                    foreach (string productSubKeyName in productsKey.GetSubKeyNames())
                    {
                        try
                        {
                            using (RegistryKey productKey = productsKey.OpenSubKey(productSubKeyName))
                            {
                                if (productKey == null)
                                    continue;

                                string productName = productKey.GetValue("ProductName")?.ToString();
                                string productIcon = productKey.GetValue("ProductIcon")?.ToString();
                                object productVersionObject = productKey.GetValue("Version");
                                string productVersion = string.Empty;
                                if (productVersionObject != null && productVersionObject is int intValue)
                                {
                                    var versionHex = $"{intValue:X}";
                                    productVersion = ParseVersion(versionHex);
                                }

                                if (string.IsNullOrEmpty(productName) || string.IsNullOrEmpty(productIcon) ||
                                    !File.Exists(productIcon) ||
                                    !string.Equals(Path.GetExtension(productIcon), ".exe", StringComparison.OrdinalIgnoreCase))
                                    continue;

                                cache[productName.ToLowerInvariant()] = new RegistryProductEntry(productName, productIcon, productVersion);
                                Logger.LogToFile($"Cached icon for ProductName: {productName}, ProductIcon: {productIcon}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogToFile($"Failed to process registry subkey {productSubKeyName}: {ex}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogToFile($"Failed to build registry icon cache: {ex}");
            }

            return cache;
        }

        public static Dictionary<string, RegistryOptionsEntry> BuildRegistryOptionsCache()
        {
            var cache = new Dictionary<string, RegistryOptionsEntry>();
            try
            {
                using (RegistryKey wdsKey = Registry.CurrentUser.OpenSubKey(@"Software\WDS LLC"))
                {
                    if (wdsKey == null)
                    {
                        Logger.LogToFile("Failed to open registry key HKLM\\SOFTWARE\\Classes\\Installer\\Products");
                        return cache;
                    }

                    foreach (string appName in wdsKey.GetSubKeyNames())
                    {
                        try
                        {
                            using (RegistryKey productKey = wdsKey.OpenSubKey(appName))
                            {
                                if (productKey == null)
                                    continue;

                                using (RegistryKey optionsKey = productKey.OpenSubKey("Options"))
                                {
                                    if (optionsKey == null)
                                        continue;

                                    var optionsEntry = new RegistryOptionsEntry(productKey.Name);
                                    foreach (string optionsValueName in optionsKey.GetValueNames())
                                    {
                                        var optionValueData = optionsKey.GetValue(optionsValueName);
                                        if (optionValueData != null && optionValueData is int intValue)
                                        {
                                            var valueHex = $"{intValue:X}";
                                            optionsEntry.Options.Add(optionsValueName,valueHex);
                                        }
                                    }

                                    cache.Add(appName, optionsEntry);

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogToFile($"Failed to process registry subkey {appName}: {ex}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogToFile($"Failed to build registry icon cache: {ex}");
            }

            return cache;
        }

        private static string ParseVersion(string hexValue)
        {
            uint value = Convert.ToUInt32(hexValue, 16);
            int major = (int)(value >> 24) & 0xFF;
            int minor = (int)(value >> 16) & 0xFF;
            int patch = (int)(value & 0xFFFF);
            return $"{major}.{minor:D2}.{patch}";
        }
    }
}
