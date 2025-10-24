using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Jellyfin.Data.Enums;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.RandomCollectionsHome
{
    public class RandomCollectionsHandler
    {
        private static ILibraryManager? _libraryManager;
        private static IDtoService? _dtoService;
        private static ILogger? _logger;
        private static PluginConfiguration? _configuration;

        public static void SetLibraryManager(ILibraryManager libraryManager, IDtoService dtoService, ILogger logger)
        {
            _libraryManager = libraryManager;
            _dtoService = dtoService;
            _logger = logger;
        }

        public static void SetConfiguration(PluginConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static QueryResult<BaseItemDto> GetCollectionItems(object payload)
        {
            try
            {
                // Log payload details for debugging
                _logger?.LogInformation("=== GetCollectionItems called ===");
                _logger?.LogInformation("Payload type: {Type}", payload?.GetType().FullName ?? "null");
                
                if (payload != null)
                {
                    if (payload is JObject jObj)
                    {
                        _logger?.LogInformation("Payload is JObject with properties: {Properties}", 
                            string.Join(", ", jObj.Properties().Select(p => $"{p.Name}={p.Value}")));
                    }
                    else
                    {
                        _logger?.LogInformation("Payload properties:");
                        var properties = payload.GetType().GetProperties();
                        foreach (var prop in properties)
                        {
                            try
                            {
                                var value = prop.GetValue(payload);
                                _logger?.LogInformation("  - {Name} = {Value}", prop.Name, value);
                            }
                            catch
                            {
                                _logger?.LogInformation("  - {Name} = (error reading)", prop.Name);
                            }
                        }
                    }
                }
                
                if (_libraryManager == null)
                {
                    _logger?.LogError("LibraryManager is null");
                    return new QueryResult<BaseItemDto>
                    {
                        Items = Array.Empty<BaseItemDto>(),
                        TotalRecordCount = 0
                    };
                }

                // Extract collection ID from payload (handles both JObject and anonymous object)
                Guid collectionId;
                if (payload is JObject payloadObj)
                {
                    // HomeScreenSections passes JObject with "AdditionalData" property as string
                    var additionalDataStr = payloadObj["AdditionalData"]?.Value<string>();
                    collectionId = Guid.TryParse(additionalDataStr, out var guid) ? guid : Guid.Empty;
                    _logger?.LogInformation("Extracted collection ID from JObject: {CollectionId}", collectionId);
                }
                else
                {
                    // Controller passes anonymous object with "AdditionalData" property
                    var idValue = payload?.GetType().GetProperty("AdditionalData")?.GetValue(payload)?.ToString();
                    collectionId = string.IsNullOrEmpty(idValue) ? Guid.Empty : Guid.Parse(idValue);
                    _logger?.LogInformation("Extracted collection ID from object property: {CollectionId}", collectionId);
                }
                
                if (collectionId == Guid.Empty)
                {
                    _logger?.LogError("Collection ID is empty - payload may not contain valid 'additionalData' field");
                    return new QueryResult<BaseItemDto>
                    {
                        Items = Array.Empty<BaseItemDto>(),
                        TotalRecordCount = 0
                    };
                }
                
                _logger?.LogDebug("Getting collection and items for {CollectionId}", collectionId);

                // Get the collection itself
                var collection = _libraryManager.GetItemById(collectionId);
                
                if (collection == null)
                {
                    _logger?.LogError("Collection {CollectionId} not found", collectionId);
                    return new QueryResult<BaseItemDto>
                    {
                        Items = Array.Empty<BaseItemDto>(),
                        TotalRecordCount = 0
                    };
                }

                // Get items within the collection
                // For BoxSet collections, we need to get the children directly from the collection
                // not by ParentId query, as collection items are linked, not actual children
                var collectionLimit = _configuration?.CollectionLimit ?? 20;
                var items = collection is MediaBrowser.Controller.Entities.Folder folder
                    ? (collectionLimit > 0 ? folder.GetRecursiveChildren().Take(collectionLimit).ToList() : folder.GetRecursiveChildren().ToList())
                    : new List<BaseItem>();

                if (_dtoService == null)
                {
                    _logger?.LogError("DtoService is null");
                    return new QueryResult<BaseItemDto>
                    {
                        Items = Array.Empty<BaseItemDto>(),
                        TotalRecordCount = 0
                    };
                }

                // Convert items to DTOs with full metadata
                var dtoOptions = new DtoOptions();
                var resultList = items.Select(item => _dtoService.GetBaseItemDto(item, dtoOptions)).ToList();

                _logger?.LogDebug("Returning collection {CollectionId} with {ItemCount} items", collectionId, items.Count);

                return new QueryResult<BaseItemDto>
                {
                    Items = resultList.ToArray(),
                    TotalRecordCount = resultList.Count
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "REMOUR Error getting collection items");
                return new QueryResult<BaseItemDto>
                {
                    Items = Array.Empty<BaseItemDto>(),
                    TotalRecordCount = 0
                };
            }
        }
    }
}
