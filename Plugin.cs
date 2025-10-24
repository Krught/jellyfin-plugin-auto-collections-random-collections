using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using System.Xml.Linq;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Jellyfin.Data.Enums;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.RandomCollectionsHome
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "Home Sections Random Collections";
        public override Guid Id => Guid.Parse("d9e7b57d-d417-4f0f-8ff9-4a6de3f42eab");
        public override string Description => "Adds random collections to the home screen each time it loads.";

        private readonly IApplicationPaths _applicationPaths;
        private readonly ILibraryManager _libraryManager;
        private readonly IDtoService _dtoService;
        private readonly ILogger<Plugin> _logger;
        private readonly Dictionary<Guid, List<Guid>> _userCache = new Dictionary<Guid, List<Guid>>();
        private readonly object _cacheLock = new object();

        public static Plugin? Instance { get; private set; }

        public Plugin(
            IApplicationPaths applicationPaths, 
            IXmlSerializer xmlSerializer, 
            ILibraryManager libraryManager,
            IDtoService dtoService,
            ILogger<Plugin> logger)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            _applicationPaths = applicationPaths;
            _libraryManager = libraryManager;
            _dtoService = dtoService;
            _logger = logger;
            RandomCollectionsHandler.SetLibraryManager(_libraryManager, _dtoService, _logger);
            RandomCollectionsHandler.SetConfiguration(Configuration);
            
            _logger.LogInformation("Home Sections Random Collections plugin initialized");
            
            // Log available collections count and register sections with delay
            try
            {
                var collections = GetAllCollections();
                _logger.LogInformation("Found {Count} collections in library", collections.Count);
                
                if (collections.Count > 0)
                {
                    foreach (var collection in collections.Take(5))
                    {
                        _logger.LogInformation("Available collection: '{Name}' (ID: {Id})", collection.Name, collection.Id);
                    }
                    if (collections.Count > 5)
                    {
                        _logger.LogInformation("... and {Count} more collections", collections.Count - 5);
                    }
                }
                
                // Wait for HomeScreenSections to be ready, then register
                Task.Run(async () =>
                {
                    await WaitForHomeScreenSectionsAsync();
                    _logger.LogInformation("HomeScreenSections is ready, starting section registration");
                    await RegisterSectionsWithRetry(Guid.Empty);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing plugin");
            }
        }

        public override void UpdateConfiguration(BasePluginConfiguration configuration)
        {
            base.UpdateConfiguration(configuration);
            
            _logger.LogInformation("Configuration updated. RandomCount: {Count}, CollectionLimit: {Limit}, CollectionUpdateInterval: {Interval}", 
                Configuration.RandomCount, Configuration.CollectionLimit, Configuration.CollectionUpdateInterval);
            
            // Update handler configuration
            RandomCollectionsHandler.SetConfiguration(Configuration);
            
            // Clear cache when configuration changes
            lock (_cacheLock)
            {
                _userCache.Clear();
            }
            
            // Re-register sections with new count
            Task.Run(async () => await RegisterSectionsWithRetry(Guid.Empty));
        }

        private List<BaseItem> GetAllCollections()
        {
            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.BoxSet },
                Recursive = true
            };

            return _libraryManager.GetItemList(query).ToList();
        }

        public List<BaseItem> GetRandomCollections(Guid userId)
        {
            try
            {
                var collections = GetAllCollections();

                if (collections.Count == 0)
                {
                    _logger.LogWarning("No collections found in library");
                    return new List<BaseItem>();
                }

                var random = new Random();
                var count = Math.Min(Configuration.RandomCount, collections.Count);
                
                _logger.LogDebug("Getting {Count} random collections from {Total} total collections for user {UserId}", 
                    count, collections.Count, userId);
                
                // Get cached collections for this user if they exist
                List<Guid>? cachedCollectionIds = null;
                lock (_cacheLock)
                {
                    if (_userCache.TryGetValue(userId, out var cached) && cached.Count == count)
                    {
                        cachedCollectionIds = cached;
                    }
                }

                // If we have cached collections and random chance (prevents too frequent changes)
                if (cachedCollectionIds != null && random.NextDouble() > 0.3) // 70% chance to use cache
                {
                    _logger.LogDebug("Using cached collections for user {UserId}", userId);
                    // Use cached collections
                    return collections.Where(c => c != null && cachedCollectionIds.Contains(c.Id)).ToList();
                }
                else
                {
                    // Pick new random collections
                    var randomCollections = collections.OrderBy(x => random.Next()).Take(count).ToList();
                    
                    _logger.LogInformation("Selected {Count} new random collections for user {UserId}", randomCollections.Count, userId);
                    
                    // Cache the new selection
                    lock (_cacheLock)
                    {
                        _userCache[userId] = randomCollections.Select(c => c.Id).ToList();
                    }
                    
                    return randomCollections;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting random collections for user {UserId}", userId);
                return new List<BaseItem>();
            }
        }

        public void ClearCacheAndReregister(Guid userId)
        {
            _logger.LogInformation("Clearing cache and re-registering sections for user {UserId}", userId);
            
            lock (_cacheLock)
            {
                _userCache.Clear();
            }
            
            Task.Run(async () => await RegisterSectionsWithRetry(userId));
        }

        public List<object> GetCurrentSections(Guid userId)
        {
            var collections = GetRandomCollections(userId);
            var result = new List<object>();
            
            int sectionIndex = 1;
            foreach (var collection in collections)
            {
                var numberedId = ConvertNumberToWord(sectionIndex);
                var uniqueId = $"RANDOM{numberedId}";
                
                // Build available view modes based on configuration
                var availableViewModes = new List<string>();
                if (Configuration.UsePortrait)
                    availableViewModes.Add("Portrait");
                if (Configuration.UseSquare)
                    availableViewModes.Add("Square");
                if (Configuration.UseLandscape)
                    availableViewModes.Add("Landscape");
                
                if (availableViewModes.Count == 0)
                {
                    availableViewModes = new List<string> { "Portrait", "Landscape", "Square" };
                }
                
                var viewMode = availableViewModes.Count == 1 
                    ? availableViewModes[0] 
                    : availableViewModes[Random.Shared.Next(availableViewModes.Count)];
                
                result.Add(new
                {
                    SectionId = uniqueId,
                    CollectionName = collection.Name,
                    CollectionId = collection.Id,
                    ViewMode = viewMode,
                    ItemCount = GetItemCount(collection)
                });
                
                sectionIndex++;
            }
            
            return result;
        }

        private int GetItemCount(BaseItem item)
        {
            try
            {
                if (item is Folder folder)
                {
                    var query = new InternalItemsQuery
                    {
                        ParentId = item.Id,
                        Recursive = true
                    };
                    return folder.GetItemList(query).Count;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task WaitForHomeScreenSectionsAsync()
        {
            int maxWaitSeconds = 60;
            int checkIntervalMs = 1000;
            int attempts = 0;
            
            _logger.LogInformation("Waiting for HomeScreenSections plugin to be ready...");
            
            while (attempts < maxWaitSeconds)
            {
                attempts++;
                
                // Check if HomeScreenSections assembly is loaded
                var assembly = AssemblyLoadContext.All
                    .SelectMany(x => x.Assemblies)
                    .FirstOrDefault(x => x.FullName?.Contains(".HomeScreenSections") ?? false);
                
                if (assembly != null)
                {
                    _logger.LogDebug("HomeScreenSections assembly found (attempt {Attempt}), checking if initialized...", attempts);
                    
                    // Check if the PluginInterface type exists
                    var pluginInterfaceType = assembly.GetType("Jellyfin.Plugin.HomeScreenSections.PluginInterface");
                    
                    if (pluginInterfaceType != null)
                    {
                        // Check if RegisterSection method exists
                        var registerMethod = pluginInterfaceType.GetMethod("RegisterSection");
                        
                        if (registerMethod != null)
                        {
                            // Try to get the Instance property to see if plugin is initialized
                            var instanceProperty = pluginInterfaceType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                            
                            if (instanceProperty != null)
                            {
                                var instance = instanceProperty.GetValue(null);
                                if (instance != null)
                                {
                                    _logger.LogInformation("HomeScreenSections plugin is fully initialized (took {Seconds} seconds)", attempts);
                                    return;
                                }
                            }
                            else
                            {
                                // If no Instance property, assume ready when method is found
                                _logger.LogInformation("HomeScreenSections plugin appears ready (took {Seconds} seconds)", attempts);
                                return;
                            }
                        }
                    }
                }
                
                if (attempts % 5 == 0)
                {
                    _logger.LogDebug("Still waiting for HomeScreenSections... ({Seconds}s elapsed)", attempts);
                }
                
                await Task.Delay(checkIntervalMs);
            }
            
            _logger.LogWarning("Timeout waiting for HomeScreenSections after {Seconds} seconds. Will attempt registration anyway.", maxWaitSeconds);
        }

        private async Task RegisterSectionsWithRetry(Guid userId)
        {
            int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Attempting to register home screen sections (attempt {Attempt}/{Max})", attempt, maxRetries);
                    await Task.Run(() => RegisterSections(userId));
                    return; // Success!
                }
                catch (Exception ex)
                {
                    if (attempt < maxRetries)
                    {
                        _logger.LogWarning(ex, "Failed to register sections (attempt {Attempt}), retrying in 2 seconds...", attempt);
                        await Task.Delay(2000);
                    }
                    else
                    {
                        _logger.LogError(ex, "Failed to register sections after {Max} attempts", maxRetries);
                    }
                }
            }
        }

        private void RegisterSections(Guid userId)
        {
            _logger.LogInformation("Attempting to register home screen sections for user {UserId}", userId);

            // Find HomeScreenSections assembly
            var homeScreenSectionsAssembly = AssemblyLoadContext.All
                .SelectMany(x => x.Assemblies)
                .FirstOrDefault(x => x.FullName?.Contains(".HomeScreenSections") ?? false);

            if (homeScreenSectionsAssembly == null)
            {
                _logger.LogWarning("HomeScreenSections plugin not found. Install it from the Jellyfin catalog to display collections on home screen.");
                _logger.LogInformation("You can still use the API endpoints: GET /RandomCollections/Get");
                return;
            }

            _logger.LogInformation("Found HomeScreenSections assembly: {Assembly}", homeScreenSectionsAssembly.FullName);

            var pluginInterfaceType = homeScreenSectionsAssembly.GetType("Jellyfin.Plugin.HomeScreenSections.PluginInterface");
            
            if (pluginInterfaceType == null)
            {
                _logger.LogError("Could not find PluginInterface type in HomeScreenSections assembly");
                return;
            }

            var registerMethod = pluginInterfaceType.GetMethod("RegisterSection");
            
            if (registerMethod == null)
            {
                _logger.LogError("Could not find RegisterSection method in HomeScreenSections.PluginInterface");
                return;
            }

            _logger.LogInformation("Successfully found RegisterSection method");

            // Get random collections to register
            var collections = GetRandomCollections(userId);

            if (collections.Count == 0)
            {
                _logger.LogWarning("No collections available to register as home screen sections");
                return;
            }

            _logger.LogInformation("Registering {Count} collections as home screen sections", collections.Count);

            // Collect all sections to update in one batch
            var sectionsToUpdate = new List<(string sectionId, string collectionName, Guid collectionId, string viewMode)>();

            // Register each collection as a section with RANDOMONE, RANDOMTWO, etc. IDs
            int sectionIndex = 1;
            foreach (var collection in collections)
            {
                // Use numbered ID pattern like RANDOMONE, RANDOMTWO, etc.
                var numberedId = ConvertNumberToWord(sectionIndex);
                var uniqueId = $"RANDOM{numberedId}";
                
                // Build available view modes based on configuration
                var availableViewModes = new List<string>();
                if (Configuration.UsePortrait)
                    availableViewModes.Add("Portrait");
                if (Configuration.UseSquare)
                    availableViewModes.Add("Square");
                if (Configuration.UseLandscape)
                    availableViewModes.Add("Landscape");
                
                // If no modes are enabled (shouldn't happen with UI validation, but safety check), default to all
                if (availableViewModes.Count == 0)
                {
                    availableViewModes = new List<string> { "Portrait", "Landscape", "Square" };
                    _logger.LogWarning("No view modes enabled in configuration, using all modes as fallback");
                }
                
                // Select view mode: if only one enabled, use it; otherwise random from available
                var viewMode = availableViewModes.Count == 1 
                    ? availableViewModes[0] 
                    : availableViewModes[Random.Shared.Next(availableViewModes.Count)];
                
                // Create JObject payload as expected by HomeScreenSections specification
                var payload = new JObject
                {
                    ["id"] = uniqueId,
                    ["displayText"] = collection.Name,
                    ["limit"] = 1,
                    ["route"] = $"/web/index.html#!/details?id={collection.Id}",
                    ["additionalData"] = collection.Id,
                    ["resultsEndpoint"] = collection.Name,
                    ["resultsAssembly"] = typeof(RandomCollectionsHandler).Assembly.FullName,
                    ["resultsClass"] = typeof(RandomCollectionsHandler).FullName,
                    ["resultsMethod"] = nameof(RandomCollectionsHandler.GetCollectionItems),
                    ["SectionViewMode"] = viewMode
                };

                _logger.LogInformation("Registering section: '{CollectionName}' with ID '{UniqueId}' (ViewMode: {ViewMode})", 
                    collection.Name, uniqueId, viewMode);
                _logger.LogDebug("Section payload - Assembly: {Assembly}, Class: {Class}, Method: {Method}, Route: {Route}", 
                    payload["resultsAssembly"], payload["resultsClass"], payload["resultsMethod"], payload["route"]);

                registerMethod.Invoke(null, new object[] { payload });
                
                _logger.LogInformation("Successfully registered section for collection '{CollectionName}'", collection.Name);
                
                // Add to batch update list
                sectionsToUpdate.Add((uniqueId, collection.Name, collection.Id, viewMode));
                sectionIndex++;
            }

            // Update all sections in the XML config file in one operation
            if (sectionsToUpdate.Count > 0)
            {
                Task.Run(async () => await UpdateAllSectionConfigurations(sectionsToUpdate));
            }

            _logger.LogInformation("Completed registering all home screen sections");
        }

        private string ConvertNumberToWord(int number)
        {
            var words = new[] { "ONE", "TWO", "THREE", "FOUR", "FIVE", "SIX", "SEVEN", "EIGHT", "NINE", "TEN",
                              "ELEVEN", "TWELVE", "THIRTEEN", "FOURTEEN", "FIFTEEN", "SIXTEEN", "SEVENTEEN", "EIGHTEEN", "NINETEEN", "TWENTY" };
            
            if (number >= 1 && number <= words.Length)
            {
                return words[number - 1];
            }
            
            // For numbers beyond 20, just use the number itself
            return number.ToString();
        }

        private async Task UpdateAllSectionConfigurations(List<(string sectionId, string collectionName, Guid collectionId, string viewMode)> sections)
        {
            await Task.CompletedTask; // Make method async-compatible
            
            try
            {
                _logger.LogInformation("Attempting to update {Count} section configurations in batch", sections.Count);
                
                // Build path to HomeScreenSections XML config file
                var pluginConfigPath = Path.Combine(_applicationPaths.PluginConfigurationsPath, "Jellyfin.Plugin.HomeScreenSections.xml");
                
                if (!File.Exists(pluginConfigPath))
                {
                    _logger.LogWarning("HomeScreenSections config file not found at: {Path}", pluginConfigPath);
                    return;
                }
                
                _logger.LogDebug("Loading HomeScreenSections config from: {Path}", pluginConfigPath);
                
                // Load the XML document
                XDocument doc = XDocument.Load(pluginConfigPath);
                
                if (doc.Root == null)
                {
                    _logger.LogWarning("Invalid XML structure in HomeScreenSections config");
                    return;
                }
                
                // Find or create the SectionSettings parent element
                var sectionSettingsParent = doc.Root.Element("SectionSettings");
                if (sectionSettingsParent == null)
                {
                    _logger.LogInformation("SectionSettings element not found, creating it");
                    sectionSettingsParent = new XElement("SectionSettings");
                    doc.Root.Add(sectionSettingsParent);
                }
                
                // FIRST: Remove all old sections that start with "RANDOM"
                var existingSections = sectionSettingsParent.Elements("SectionSettings").ToList();
                var oldRandomSections = existingSections
                    .Where(s => s.Element("SectionId")?.Value?.StartsWith("RANDOM") == true)
                    .ToList();
                
                if (oldRandomSections.Any())
                {
                    _logger.LogInformation("Removing {Count} old RANDOM* sections from config", oldRandomSections.Count);
                    foreach (var oldSection in oldRandomSections)
                    {
                        var oldSectionId = oldSection.Element("SectionId")?.Value;
                        _logger.LogDebug("Removing old section: {SectionId}", oldSectionId);
                        oldSection.Remove();
                    }
                }
                
                // SECOND: Create all new sections with updated settings
                int createdCount = 0;
                
                foreach (var (sectionId, collectionName, collectionId, viewMode) in sections)
                {
                    _logger.LogDebug("Creating new section with ID '{SectionId}' for collection '{CollectionName}' (ViewMode: {ViewMode})", 
                        sectionId, collectionName, viewMode);
                    
                    // Create new section element
                    var newSection = new XElement("SectionSettings",
                        new XElement("SectionId", sectionId),
                        new XElement("Enabled", "true"),
                        new XElement("AllowUserOverride", "false"),
                        new XElement("LowerLimit", "1"),
                        new XElement("UpperLimit", "1"),
                        new XElement("OrderIndex", "999"),
                        new XElement("ViewMode", viewMode),
                        new XElement("HideWatchedItems", "false")
                    );
                    
                    sectionSettingsParent.Add(newSection);
                    createdCount++;
                }
                
                // Save the XML file once
                doc.Save(pluginConfigPath);
                _logger.LogInformation("Successfully saved HomeScreenSections config: {Removed} removed, {Created} created", 
                    oldRandomSections.Count, createdCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating section configurations in batch");
            }
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = this.Name,
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
                }
            };
        }
    }
}
