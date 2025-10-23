using System;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Querying;
using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.RandomCollectionsHome
{
    public class RandomCollectionsHandler
    {
        private static ILibraryManager? _libraryManager;

        public static void SetLibraryManager(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        public static QueryResult<BaseItemDto> GetCollectionItems(object payload)
        {
            if (_libraryManager == null)
            {
                return new QueryResult<BaseItemDto>
                {
                    Items = Array.Empty<BaseItemDto>(),
                    TotalRecordCount = 0
                };
            }

            var collectionId = Guid.Parse(payload?.GetType().GetProperty("AdditionalData")?.GetValue(payload)?.ToString() ?? Guid.Empty.ToString());

            var query = new InternalItemsQuery
            {
                ParentId = collectionId
            };

            var items = _libraryManager.GetItemList(query);

            // Convert BaseItem to BaseItemDto - simplified approach
            var dtoItems = items.Select(item => new BaseItemDto
            {
                Id = item.Id,
                Name = item.Name,
                Type = BaseItemKind.Movie // Default type, can be adjusted
            }).ToList();

            return new QueryResult<BaseItemDto>
            {
                Items = dtoItems.ToArray(),
                TotalRecordCount = dtoItems.Count()
            };
        }
    }
}
