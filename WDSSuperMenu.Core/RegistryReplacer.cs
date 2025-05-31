using Microsoft.Win32;

namespace WDSSuperMenu.Core
{
    public class RegistryReplacer
    {
        public static void CopySettings(string sourceApp, string targetApp)
        {
            using (RegistryKey sourceKey = Registry.CurrentUser.OpenSubKey($@"Software\WDS LLC\{sourceApp}\Options"))
            using (RegistryKey targetKey = Registry.CurrentUser.OpenSubKey($@"Software\WDS LLC\{targetApp}\Options", true))
            {
                if (sourceKey == null) throw new ArgumentException("Source application not found");
                if (targetKey == null) throw new ArgumentException("Target application not found");

                foreach (string valueName in sourceKey.GetValueNames())
                {
                    // Only copy if target already has this value (your original logic)
                    if (targetKey.GetValue(valueName) != null)
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
