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
        public static List<string> GetAppsByPublisher()
        {
            var installLocations = new List<string>();
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
                                            installLocations.Add(location);
                                    }
                                }
                            }
                            productsKey.Dispose();
                        }
                    }
                    userDataKey.Dispose();
                }
            }
            var uniqueParentDirectories = new List<string>();
            var roots = installLocations.Select(x => GetParentDirectory(x.ToLower())).Distinct();
            foreach (var path in roots)
            {
                string[] subdirectories = Directory.GetDirectories(path);

                foreach (var subdir in subdirectories)
                {
                   var exeFiles = Directory.GetFiles(subdir, "*.exe", SearchOption.TopDirectoryOnly)
                         .Where(file =>
                         {
                             string lower = Path.GetFileName(file).ToLowerInvariant();
                             return true;
                         })
                         .ToList();

                    uniqueParentDirectories.AddRange(exeFiles);
                }
            }

            return uniqueParentDirectories;
        }

        private static string GetParentDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                var dirInfo = new DirectoryInfo(path.Trim());
                return dirInfo.Parent?.FullName;
            }
            catch
            {
                // Handle invalid paths gracefully
                return null;
            }
        }
    }

}
