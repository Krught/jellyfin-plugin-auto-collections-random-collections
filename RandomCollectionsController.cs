using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities;
using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.RandomCollectionsHome
{
    /// <summary>
    /// Random Collections API Controller
    /// </summary>
    [ApiController]
    [Route("RandomCollections")]
    [Authorize]
    public class RandomCollectionsController : ControllerBase
    {
        private readonly ILogger<RandomCollectionsController> _logger;

        public RandomCollectionsController(ILogger<RandomCollectionsController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets random collections for the current user
        /// </summary>
        /// <returns>List of random collection IDs and names</returns>
        [HttpGet("Get")]
        public ActionResult<IEnumerable<CollectionInfo>> GetRandomCollections()
        {
            try
            {
                if (Plugin.Instance == null)
                {
                    _logger.LogError("Plugin instance is null");
                    return StatusCode(500, "Plugin not initialized");
                }

                // Get user ID from the authenticated request
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = string.IsNullOrEmpty(userIdClaim) ? Guid.Empty : Guid.Parse(userIdClaim);
                
                _logger.LogInformation("Getting random collections for user {UserId}", userId);

                var collections = Plugin.Instance.GetRandomCollections(userId);

                var result = collections.Select(c => new CollectionInfo
                {
                    Id = c.Id,
                    Name = c.Name ?? "Unknown",
                    ItemCount = GetItemCount(c)
                }).ToList();

                _logger.LogInformation("Returning {Count} random collections", result.Count);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting random collections");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Gets items from a specific collection
        /// </summary>
        /// <param name="collectionId">The collection ID</param>
        /// <returns>Query result with collection items</returns>
        [HttpGet("Items/{collectionId}")]
        public ActionResult<QueryResult<MediaBrowser.Model.Dto.BaseItemDto>> GetCollectionItems([FromRoute] Guid collectionId)
        {
            try
            {
                _logger.LogInformation("Getting items for collection {CollectionId}", collectionId);

                var payload = new { AdditionalData = collectionId.ToString() };
                var result = RandomCollectionsHandler.GetCollectionItems(payload);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting collection items for {CollectionId}", collectionId);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Clears the cache and forces new random selection
        /// </summary>
        [HttpPost("Refresh")]
        public ActionResult Refresh()
        {
            try
            {
                _logger.LogInformation("Refreshing random collections cache");
                
                if (Plugin.Instance == null)
                {
                    _logger.LogError("Plugin instance is null");
                    return StatusCode(500, "Plugin not initialized");
                }
                
                // Get user ID from the authenticated request
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = string.IsNullOrEmpty(userIdClaim) ? Guid.Empty : Guid.Parse(userIdClaim);
                
                // Clear cache and re-register sections
                Plugin.Instance.ClearCacheAndReregister(userId);
                
                return Ok(new { message = "Cache cleared and sections re-registered successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing collections");
                return StatusCode(500, ex.Message);
            }
        }
        
        /// <summary>
        /// Gets current section information for debugging
        /// </summary>
        [HttpGet("Debug/Sections")]
        public ActionResult<object> GetDebugSections()
        {
            try
            {
                if (Plugin.Instance == null)
                {
                    return StatusCode(500, "Plugin not initialized");
                }
                
                // Get user ID from the authenticated request
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = string.IsNullOrEmpty(userIdClaim) ? Guid.Empty : Guid.Parse(userIdClaim);
                
                var sections = Plugin.Instance.GetCurrentSections(userId);
                
                return Ok(sections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting debug sections");
                return StatusCode(500, ex.Message);
            }
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
    }

    public class CollectionInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ItemCount { get; set; }
    }
}

