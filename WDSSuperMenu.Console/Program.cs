// See https://aka.ms/new-console-template for more information
using WDSSuperMenu;

Console.WriteLine("Hello, World!");

var adobeApps = RegistryAppFinder.GetAppsByPublisher("WDS LLC");

Console.ReadKey();