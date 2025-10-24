using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Jellyfin.Data.Enums;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.RandomCollectionsHome
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "Auto Collections Random Collections";
        public override Guid Id => Guid.Parse("d9e7b57d-d417-4f0f-8ff9-4a6de3f42eab");
        public override string Description => "Adds random collections to the home screen each time it loads.";

        private readonly ILibraryManager _libraryManager;
        private readonly IDtoService _dtoService;
        private readonly ILogger<Plugin> _logger;
        private readonly Dictionary<Guid, List<Guid>> _userCache = new Dictionary<Guid, List<Guid>>();
        private readonly object _cacheLock = new object();
        private static readonly HttpClient _httpClient = new HttpClient();

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
            _libraryManager = libraryManager;
            _dtoService = dtoService;
            _logger = logger;
            RandomCollectionsHandler.SetLibraryManager(_libraryManager, _dtoService, _logger);
            
            _logger.LogInformation("Random Collections Home plugin initialized");
            
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
            
            _logger.LogInformation("Configuration updated. RandomCount: {Count}", Configuration.RandomCount);
            
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

            // Build sections for configuration POST
            var configSections = new List<object>();

            // Register each collection as a section
            foreach (var collection in collections)
            {
                var uniqueId = collection.Name.Replace(" ", "").Replace("-", "").Replace("_", "").Replace(".", "").Replace("'", "").Replace("\"", "").Replace("&", "");
                
                var viewModes = new[] { "Portrait", "Landscape", "Square" };
                var viewMode = viewModes[Random.Shared.Next(viewModes.Length)];
                
                // Create JObject payload as expected by HomeScreenSections specification
                var payload = new JObject
                {
                    // ["id"] = collection.Id,
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

                _logger.LogInformation("Registering section: '{CollectionName}' (ID: {CollectionId})", 
                    collection.Name, collection.Id);
                _logger.LogDebug("Section payload - Assembly: {Assembly}, Class: {Class}, Method: {Method}, Route: {Route}", 
                    payload["resultsAssembly"], payload["resultsClass"], payload["resultsMethod"], payload["route"]);

                registerMethod.Invoke(null, new object[] { payload });
                
                _logger.LogInformation("Successfully registered section for collection '{CollectionName}'", collection.Name);
                
                // Add to configuration sections list
                configSections.Add(new
                {
                    UniqueId = uniqueId,
                    DisplayText = collection.Name,
                    CollectionName = collection.Name,
                    SectionType = "Collection"
                });
            }

            // POST to configuration endpoint
            // Task.Run(async () => await PostToConfigurationEndpoint(configSections));

            _logger.LogInformation("Completed registering all home screen sections");
        }

        private async Task PostToConfigurationEndpoint(List<object> sections)
        {
            try
            {
                // HomeScreenSections plugin GUID
                var homeScreenSectionsPluginId = "043b2c48-b3e0-4610-b398-8217b146d1a4";
                var configEndpoint = $"http://localhost:8096/Plugins/{homeScreenSectionsPluginId}/Configuration";
                
                var configPayload = new
                {
                    Sections = sections
                };

                var jsonContent = JsonConvert.SerializeObject(configPayload);
                _logger.LogInformation("POSTing configuration to {Endpoint}", configEndpoint);
                _logger.LogDebug("Configuration payload: {Payload}", jsonContent);

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(configEndpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully posted configuration with {Count} sections", sections.Count);
                }
                else
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to post configuration. Status: {Status}, Response: {Response}", 
                        response.StatusCode, responseBody);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting to configuration endpoint");
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
