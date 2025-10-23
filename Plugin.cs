using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Serialization;
using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.RandomCollectionsHome
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "Auto-Collections-Random-Collections";
        public override string Description => "Adds random collections to the home screen each time it loads.";

        private readonly ILibraryManager _libraryManager;
        private readonly Dictionary<Guid, List<Guid>> _userCache = new Dictionary<Guid, List<Guid>>();
        private readonly object _cacheLock = new object();

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILibraryManager libraryManager)
            : base(applicationPaths, xmlSerializer)
        {
            _libraryManager = libraryManager;
            RandomCollectionsHandler.SetLibraryManager(_libraryManager);
            
            // Register initial sections on startup
            RegisterSections(Guid.Empty);
        }

        public override void UpdateConfiguration(BasePluginConfiguration configuration)
        {
            base.UpdateConfiguration(configuration);
            
            // Clear cache when configuration changes
            lock (_cacheLock)
            {
                _userCache.Clear();
            }
            
            // Re-register sections with new count
            RegisterSections(Guid.Empty);
        }

        /// <summary>
        /// Called when the home screen is loaded - regenerates random collections
        /// </summary>
        public void OnHomeScreenLoad(Guid userId)
        {
            RegisterSections(userId);
        }

        private void RegisterSections(Guid userId)
        {
            try
            {
                var query = new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.BoxSet },
                    Recursive = true
                };

                var collections = _libraryManager.GetItemList(query).ToList();

                if (collections.Count == 0) return;

                var random = new Random();
                var count = Math.Min(Configuration.RandomCount, collections.Count);
                
                // Get cached collections for this user if they exist and haven't expired
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
                    // Use cached collections
                    collections = collections.Where(c => c != null && cachedCollectionIds.Contains(c.Id)).ToList();
                }
                else
                {
                    // Pick new random collections
                    var randomCollections = collections.OrderBy(x => random.Next()).Take(count).ToList();
                    
                    // Cache the new selection
                    lock (_cacheLock)
                    {
                        _userCache[userId] = randomCollections.Select(c => c.Id).ToList();
                    }
                    
                    collections = randomCollections;
                }

                var homeAssembly = AssemblyLoadContext.All
                    .SelectMany(x => x.Assemblies)
                    .FirstOrDefault(x => x.FullName?.Contains(".HomeScreenSections") ?? false);

                if (homeAssembly == null) return;

                var pluginInterfaceType = homeAssembly.GetType("Jellyfin.Plugin.HomeScreenSections.PluginInterface");
                var registerMethod = pluginInterfaceType?.GetMethod("RegisterSection");
                if (registerMethod == null) return;

                foreach (var col in collections)
                {
                    var payload = new
                    {
                        id = Guid.NewGuid(),
                        displayText = col.Name,
                        limit = 1,
                        route = $"/collections/{col.Id}",
                        additionalData = col.Id.ToString(),
                        resultsAssembly = typeof(RandomCollectionsHandler).Assembly.FullName,
                        resultsClass = nameof(RandomCollectionsHandler),
                        resultsMethod = nameof(RandomCollectionsHandler.GetCollectionItems)
                    };

                    registerMethod.Invoke(null, new object[] { payload });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RandomCollectionsHome] Error registering sections: {ex}");
            }
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "RandomCollectionsHome",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
                }
            };
        }
    }
}
