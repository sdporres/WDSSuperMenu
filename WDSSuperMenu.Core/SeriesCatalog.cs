using System.Text.Json;

namespace WDSSuperMenu.Core
{
    public static class SeriesCatalog
    {
        private static Dictionary<string, List<string>> _seriesTitles;
        private static readonly object _lock = new object();
        private const string LocalSeriesDataFile = "series_data.json";
        private const string GitHubRawUrl = "https://raw.githubusercontent.com/sdporres/wdssupermenu-data/main/series_data.json";
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(24); // Cache for 24 hours

        public static string FindSeriesForGame(string gameTitle)
        {
            var seriesTitles = SeriesTitles;
            foreach (var series in seriesTitles)
            {
                if (series.Value.Any(title => gameTitle.Contains(title, StringComparison.OrdinalIgnoreCase)))
                {
                    return series.Key;
                }
            }
            return null; // Game not found in any series
        }

        public static Dictionary<string, List<string>> SeriesTitles
        {
            get
            {
                if (_seriesTitles == null)
                {
                    lock (_lock)
                    {
                        if (_seriesTitles == null)
                        {
                            // If this is called before LoadSeriesDataAsync completes,
                            // return fallback data temporarily
                            _seriesTitles = GetFallbackSeriesData();
                        }
                    }
                }
                return _seriesTitles;
            }
            private set
            {
                lock (_lock)
                {
                    _seriesTitles = value;
                }
            }
        }
        public static async Task<Dictionary<string, List<string>>> LoadSeriesDataAsync()
        {
            if (_seriesTitles != null)
            {
                return _seriesTitles; // Already loaded
            }

            string localFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LocalSeriesDataFile);

            try
            {
                // Try to download from GitHub first
                var downloadedData = await TryDownloadFromGitHubAsync(localFilePath);
                if (downloadedData != null)
                {
                    lock (_lock)
                    {
                        _seriesTitles = downloadedData;
                    }
                    Logger.LogToFile("Successfully loaded series data from GitHub");
                    return downloadedData;
                }

                // Fall back to local file
                if (File.Exists(localFilePath))
                {
                    var localData = await LoadFromLocalFileAsync(localFilePath);
                    if (localData != null)
                    {
                        lock (_lock)
                        {
                            _seriesTitles = localData;
                        }
                        Logger.LogToFile("Loaded series data from local file (GitHub unavailable)");
                        return localData;
                    }
                }

                // Create default file if nothing exists
                Logger.LogToFile("No series data found. Creating default local file.");
                await CreateDefaultSeriesDataFileAsync(localFilePath).ConfigureAwait(false);
                var fallbackData = GetFallbackSeriesData();
                lock (_lock)
                {
                    _seriesTitles = fallbackData;
                }
                return fallbackData;
            }
            catch (Exception ex)
            {
                Logger.LogToFile($"Error loading series data: {ex.Message}. Using fallback data.");
                var fallbackData = GetFallbackSeriesData();
                lock (_lock)
                {
                    _seriesTitles = fallbackData;
                }
                return fallbackData;
            }
        }

        private static async Task<Dictionary<string, List<string>>> TryDownloadFromGitHubAsync(string localFilePath)
        {
            try
            {
                // Check if we should download (cache expired or no local file)
                if (!ShouldDownloadFromGitHub(localFilePath))
                {
                    return null;
                }

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10); // 10 second timeout
                httpClient.DefaultRequestHeaders.Add("User-Agent", "WDS-Super-Menu/1.0");

                Logger.LogToFile($"Attempting to download series data from: {GitHubRawUrl}");

                string jsonContent = await httpClient.GetStringAsync(GitHubRawUrl);

                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    Logger.LogToFile("Downloaded content is empty");
                    return null;
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                var seriesData = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonContent, options);

                if (seriesData == null || seriesData.Count == 0)
                {
                    Logger.LogToFile("Downloaded series data is invalid or empty");
                    return null;
                }

                // Save downloaded data locally for caching
                await File.WriteAllTextAsync(localFilePath, jsonContent);
                Logger.LogToFile($"Successfully downloaded and cached {seriesData.Count} series from GitHub");

                return seriesData;
            }
            catch (HttpRequestException ex)
            {
                Logger.LogToFile($"Network error downloading from GitHub: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                Logger.LogToFile($"Timeout downloading from GitHub: {ex.Message}");
                return null;
            }
            catch (JsonException ex)
            {
                Logger.LogToFile($"Invalid JSON from GitHub: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogToFile($"Unexpected error downloading from GitHub: {ex.Message}");
                return null;
            }
        }

        private static bool ShouldDownloadFromGitHub(string localFilePath)
        {
            try
            {
                if (!File.Exists(localFilePath))
                {
                    return true; // No local file, must download
                }

                var fileInfo = new FileInfo(localFilePath);
                var timeSinceLastUpdate = DateTime.Now - fileInfo.LastWriteTime;

                bool shouldDownload = timeSinceLastUpdate > CacheExpiry;
                Logger.LogToFile($"Local file age: {timeSinceLastUpdate.TotalHours:F1} hours. Should download: {shouldDownload}");

                return shouldDownload;
            }
            catch (Exception ex)
            {
                Logger.LogToFile($"Error checking local file age: {ex.Message}");
                return true; // When in doubt, try to download
            }
        }

        private static async Task<Dictionary<string, List<string>>> LoadFromLocalFileAsync(string filePath)
        {
            try
            {
                string jsonContent = await File.ReadAllTextAsync(filePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                var seriesData = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonContent, options);

                if (seriesData == null || seriesData.Count == 0)
                {
                    Logger.LogToFile("Local series data file is invalid or empty");
                    return null;
                }

                Logger.LogToFile($"Successfully loaded {seriesData.Count} series from local file");
                return seriesData;
            }
            catch (Exception ex)
            {
                Logger.LogToFile($"Error loading local series data: {ex.Message}");
                return null;
            }
        }

        private static async Task CreateDefaultSeriesDataFileAsync(string filePath)
        {
            try
            {
                var defaultData = GetFallbackSeriesData();
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string jsonContent = JsonSerializer.Serialize(defaultData, options);
                await File.WriteAllTextAsync(filePath, jsonContent);
                Logger.LogToFile($"Created default series data file at {filePath}");
            }
            catch (Exception ex)
            {
                Logger.LogToFile($"Failed to create default series data file: {ex.Message}");
            }
        }

        /// <summary>
        /// Forces a reload of series data from GitHub, bypassing cache
        /// </summary>
        public static async Task ForceReloadFromGitHubAsync()
        {
            lock (_lock)
            {
                _seriesTitles = null;
            }

            try
            {
                string localFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LocalSeriesDataFile);

                // Delete local file to force download
                if (File.Exists(localFilePath))
                {
                    File.Delete(localFilePath);
                }

                // Trigger reload
                var _ = SeriesTitles;
                Logger.LogToFile("Forced reload of series data from GitHub completed");
            }
            catch (Exception ex)
            {
                Logger.LogToFile($"Error during forced reload: {ex.Message}");
            }
        }

        /// <summary>
        /// Reloads the series data, checking GitHub if cache has expired
        /// </summary>
        public static async Task ReloadSeriesDataAsync()
        {
            lock (_lock)
            {
                _seriesTitles = null;
            }

            // Trigger reload
            var _ = SeriesTitles;
            Logger.LogToFile("Series data reloaded");
        }

        /// <summary>
        /// Gets the path to the local series data file
        /// </summary>
        public static string GetLocalSeriesDataFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LocalSeriesDataFile);
        }

        /// <summary>
        /// Gets the GitHub URL for the series data
        /// </summary>
        public static string GetGitHubUrl()
        {
            return GitHubRawUrl;
        }

        /// <summary>
        /// Checks if the local cache is expired
        /// </summary>
        public static bool IsLocalCacheExpired()
        {
            string localFilePath = GetLocalSeriesDataFilePath();
            return ShouldDownloadFromGitHub(localFilePath);
        }

        /// <summary>
        /// Gets information about the current data source
        /// </summary>
        public static async Task<DataSourceInfo> GetDataSourceInfoAsync()
        {
            string localFilePath = GetLocalSeriesDataFilePath();
            var info = new DataSourceInfo();

            if (File.Exists(localFilePath))
            {
                var fileInfo = new FileInfo(localFilePath);
                info.LocalFileExists = true;
                info.LocalFileLastModified = fileInfo.LastWriteTime;
                info.LocalFileAge = DateTime.Now - fileInfo.LastWriteTime;
                info.IsCacheExpired = info.LocalFileAge > CacheExpiry;
            }

            // Test GitHub connectivity
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                var response = await httpClient.GetAsync(GitHubRawUrl, HttpCompletionOption.ResponseHeadersRead);
                info.GitHubAccessible = response.IsSuccessStatusCode;
                info.GitHubLastModified = response.Content.Headers.LastModified?.DateTime;
            }
            catch
            {
                info.GitHubAccessible = false;
            }

            return info;
        }

        private static Dictionary<string, List<string>> GetFallbackSeriesData()
        {
            return new Dictionary<string, List<string>>()
            {
                ["Panzer Campaigns"] = new List<string>
                {
                    "Budapest '45", "Bulge '44", "El Alamein '42", "France '40", "Japan '45", "Japan '46",
                    "Kharkov '42", "Kharkov '43", "Kiev '43", "Korsun '44", "Kursk '43", "Market-Garden '44",
                    "Minsk '44", "Mius '43", "Moscow '41", "Moscow '42", "Normandy '44", "Orel '43",
                    "Philippines '44", "Poland '39", "Rumyantsev '43", "Rzhev '42", "Salerno '43",
                    "Scheldt '44", "Sealion '40", "Sicily '43", "Smolensk '41", "Smolensk '43",
                    "Spring Awakening '45", "Stalingrad '42", "Tobruk '41", "Tunisia '43"
                },
                ["Musket and Pike"] = new List<string>
                {
                    "Great Northern War", "Renaissance", "Seven Years War", "Thirty Years War",
                    "Vienna 1683", "War of the Austrian Succession"
                },
                ["Napoleonic Battles"] = new List<string>
                {
                    "Bonaparte's Peninsular War", "Campaign 1814", "Campaign Austerlitz", "Campaign Bautzen",
                    "Campaign Eckmuhl", "Campaign Eylau", "Campaign Jena", "Campaign Leipzig", "Campaign Marengo",
                    "Campaign Wagram", "Campaign Waterloo", "Napoleon's Russian Campaign",
                    "Republican Bayonets on the Rhine", "The Final Struggle", "Wellington's Peninsular War"
                },
                ["Civil War Battles"] = new List<string>
                {
                    "Campaign Antietam", "Campaign Atlanta", "Campaign Chancellorsville", "Campaign Chickamauga",
                    "Campaign Corinth", "Campaign Franklin", "Campaign Gettysburg", "Campaign Overland",
                    "Campaign Ozark", "Campaign Peninsula", "Campaign Petersburg", "Campaign Shenandoah",
                    "Campaign Shiloh", "Campaign Vicksburg", "Civil War Battles Demo", "Forgotten Campaigns"
                },
                ["Naval Campaigns"] = new List<string>
                {
                    "Guadalcanal Naval Battles", "Jutland", "Kriegsmarine", "Midway", "Tsushima", "Wolfpack"
                },
                ["Early American Wars"] = new List<string>
                {
                    "Campaign 1776", "Little Big Horn", "Mexican-American War",
                    "The French and Indian War", "The War of 1812"
                },
                ["Panzer Battles"] = new List<string>
                {
                    "Battles of Kursk - Southern Flank", "Battles of Normandy",
                    "Battles of North Africa 1941", "Panzer Battles Demo"
                },
                ["First World War Campaigns"] = new List<string>
                {
                    "East Prussia '14", "France '14", "Serbia '14"
                },
                ["Strategic War"] = new List<string>
                {
                    "The First Blitzkrieg", "War on the Southern Front"
                },
                ["Modern Air Power"] = new List<string>
                {
                    "War Over The Mideast", "War Over Vietnam", "Modern Air Power Demo"
                },
                ["Sword and Siege"] = new List<string>
                {
                    "Sword & Siege Demo", "Crusades: Book I"
                },
                ["Modern Campaigns"] = new List<string>
                {
                    "Danube Front '85", "Fulda Gap '85", "Korea '85", "Middle East '67",
                    "North German Plain '85", "Quang Tri '72"
                }
            };
        }
    }

    public class DataSourceInfo
    {
        public bool LocalFileExists { get; set; }
        public DateTime? LocalFileLastModified { get; set; }
        public TimeSpan LocalFileAge { get; set; }
        public bool IsCacheExpired { get; set; }
        public bool GitHubAccessible { get; set; }
        public DateTime? GitHubLastModified { get; set; }
    }
}