using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDSSuperMenu
{
    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;

    public class InstalledAppInfo
    {
        public string DisplayName { get; set; }
        public string Publisher { get; set; }
        public string InstallLocation { get; set; }
    }

    public static class RegistryAppFinder
    {
        public static List<string> FindUniqueParentDirectories()
        {
            var uniqueParentDirectories = new HashSet<string>();
            const string targetPublisher = "Wargame Design Studio";

            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(
                RegistryHive.LocalMachine,
                RegistryView.Registry64))
            {
                RegistryKey userDataKey = baseKey.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData");

                if (userDataKey != null)
                {
                    foreach (string userSid in userDataKey.GetSubKeyNames())
                    {
                        RegistryKey productsKey = userDataKey.OpenSubKey(
                            $@"{userSid}\Products");

                        if (productsKey != null)
                        {
                            foreach (string productGuid in productsKey.GetSubKeyNames())
                            {
                                using (RegistryKey installProps = productsKey.OpenSubKey(
                                    $@"{productGuid}\InstallProperties"))
                                {
                                    if (installProps?.GetValue("DisplayName") as string == "WDS Menu") continue;

                                    if (installProps?.GetValue("Publisher") as string == targetPublisher)
                                    {
                                        string location = installProps.GetValue("InstallLocation") as string;
                                        if (!string.IsNullOrEmpty(location))
                                        {
                                            string parentDir = GetParentDirectory(location);
                                            if (!string.IsNullOrEmpty(parentDir))
                                                uniqueParentDirectories.Add(parentDir);
                                        }
                                    }
                                }
                            }
                            productsKey.Dispose();
                        }
                    }
                    userDataKey.Dispose();
                }
            }
            return new List<string>(uniqueParentDirectories);
        }

        private static string GetParentDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                var dirInfo = new DirectoryInfo(path.Trim());
                var parent = dirInfo.Parent?.FullName;
                if (parent == null)
                    return null;

                // Normalize: lower case, trim trailing separators
                parent = parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                               .ToLowerInvariant();

                // Optional: Skip root directories (e.g., "e:")
                if (parent.Length == 2 && parent[1] == ':')
                    return null;

                return parent;
            }
            catch
            {
                return null;
            }
        }
    }

}
