namespace WDSSuperMenu.Core
{
    public static class SeriesCatalog
    {
        public static string FindSeriesForGame(string gameTitle)
        {
            foreach (var series in SeriesTitles)
            {
                if (series.Value.Any(title => gameTitle.Contains(title, StringComparison.OrdinalIgnoreCase)))
                {
                    return series.Key;
                }
            }
            return null; // Game not found in any series
        }

        public static Dictionary<string, List<string>> SeriesTitles { get; } = new Dictionary<string, List<string>>()
        {
            ["Panzer Campaigns"] = new List<string>
            {
                "Budapest '45",
                "Bulge '44",
                "El Alamein '42",
                "France '40",
                "Japan '45",
                "Japan '46",
                "Kharkov '42",
                "Kharkov '43",
                "Kiev '43",
                "Korsun '44",
                "Kursk '43",
                "Market-Garden '44",
                "Minsk '44",
                "Mius '43",
                "Moscow '41",
                "Moscow '42",
                "Normandy '44",
                "Orel '43",
                "Philippines '44",
                "Poland '39",
                "Rumyantsev '43",
                "Rzhev '42",
                "Salerno '43",
                "Scheldt '44",
                "Sealion '40",
                "Sicily '43",
                "Smolensk '41",
                "Smolensk '43",
                "Spring Awakening '45",
                "Stalingrad '42",
                "Tobruk '41",
                "Tunisia '43"
            },

            ["Musket and Pike"] = new List<string>
            {
                "Great Northern War",
                "Renaissance",
                "Seven Years War",
                "Thirty Years War",
                "War of the Austrian Succession"
            },

            ["Napoleonic Battles"] = new List<string>
            {
                "Bonaparte's Peninsular War",
                "Campaign 1814",
                "Campaign Austerlitz",
                "Campaign Bautzen",
                "Campaign Eckmuhl",
                "Campaign Eylau",
                "Campaign Jena",
                "Campaign Leipzig",
                "Campaign Marengo",
                "Campaign Wagram",
                "Campaign Waterloo",
                "Napoleon's Russian Campaign",
                "Republican Bayonets on the Rhine",
                "The Final Struggle",
                "Wellington's Peninsular War"
            },

            ["Civil War Battles"] = new List<string>
            {
                "Campaign Antietam",
                "Campaign Atlanta",
                "Campaign Chancellorsville",
                "Campaign Chickamauga",
                "Campaign Corinth",
                "Campaign Franklin",
                "Campaign Gettysburg",
                "Campaign Overland",
                "Campaign Ozark",
                "Campaign Peninsula",
                "Campaign Petersburg",
                "Campaign Shenandoah",
                "Campaign Shiloh",
                "Campaign Vicksburg",
                "Civil War Battles Demo",
                "Forgotten Campaigns"
            },

            ["Naval Campaigns"] = new List<string>
            {
                "Guadalcanal Naval Battles",
                "Jutland",
                "Kriegsmarine",
                "Midway",
                "Tsushima",
                "Wolfpack"
            },

            ["Early American Wars"] = new List<string>
            {
                "Campaign 1776",
                "Little Big Horn",
                "Mexican-American War",
                "The French and Indian War",
                "The War of 1812"
            },

            ["Panzer Battles"] = new List<string>
            {
                "Battles of Kursk - Southern Flank",
                "Battles of Normandy",
                "Battles of North Africa 1941",
                "Panzer Battles Demo"
            },

            ["First World War Campaigns"] = new List<string>
            {
                "East Prussia '14",
                "France '14",
                "Serbia '14"
            },

            ["Strategic War"] = new List<string>
            {
                "The First Blitzkrieg",
                "War on the Southern Front"
            },

            ["Modern Air Power"] = new List<string>
            {
                "War Over The Mideast",
                "War Over Vietnam",
                "Modern Air Power Demo"
            },

            ["Sword and Siege"] = new List<string>
            {
                "Sword & Siege Demo",
                "Crusades: Book I"
            },

            ["Modern Campaigns"] = new List<string>
            {
                "Danube Front '85",
                "Fulda Gap '85",
                "Korea '85",
                "Middle East '67",
                "North German Plain '85",
                "Quang Tri '72"
            }
        };
    }
}
