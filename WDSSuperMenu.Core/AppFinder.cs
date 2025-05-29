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
        public static List<InstalledAppInfo> GetAppsByPublisher(string publisherName)
        {
            string folderName = "WDS";
            var foundPaths = new List<string>();

            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady || drive.DriveType != DriveType.Fixed)
                    continue;

                string potentialPath = Path.Combine(drive.RootDirectory.FullName, folderName);

                if (Directory.Exists(potentialPath))
                {
                    foundPaths.Add(potentialPath);
                }
            }

            foreach (var path in foundPaths)
            {
                string[] subdirectories = Directory.GetDirectories(path);

                foreach (var subdir in subdirectories)
                {
                    string[] exeFiles = Directory.GetFiles(subdir, "*.exe", SearchOption.TopDirectoryOnly)
                         .Where(file =>
                         {
                             string lower = Path.GetFileName(file).ToLowerInvariant();
                             return true;
                         })
                         .ToArray();
                }
            }

            return null;
        }
    }

}
