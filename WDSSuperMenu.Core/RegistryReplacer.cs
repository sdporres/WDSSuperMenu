using Microsoft.Win32;

namespace WDSSuperMenu.Core
{
    public class RegistryReplacer
    {
        public static void CopySettings(string sourceApp, string targetApp)
        {
            using (RegistryKey sourceKey = Registry.CurrentUser.OpenSubKey($@"Software\WDS LLC\{sourceApp}\Options"))
            {
                if (sourceKey == null)
                    throw new ArgumentException("Source application not found");

                using (RegistryKey targetKey = Registry.CurrentUser.CreateSubKey($@"Software\WDS LLC\{targetApp}\Options"))
                {
                    if (targetKey == null)
                        throw new InvalidOperationException("Failed to create or open target application key");

                    foreach (string valueName in sourceKey.GetValueNames())
                    {
                        object sourceValue = sourceKey.GetValue(valueName);
                        RegistryValueKind valueType = sourceKey.GetValueKind(valueName);
                        targetKey.SetValue(valueName, sourceValue, valueType);
                    }
                }
            }
        }
    }
}
