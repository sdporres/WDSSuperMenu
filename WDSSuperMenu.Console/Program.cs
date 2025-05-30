// See https://aka.ms/new-console-template for more information
using WDSSuperMenu;

Console.WriteLine("Hello, World!");

var publisherApps = RegistryAppFinder.GetAppsByPublisher();

Console.ReadKey();