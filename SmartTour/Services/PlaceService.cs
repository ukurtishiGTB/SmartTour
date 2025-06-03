// File: SmartTour/Data/Services/PlaceService.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ArangoDB driver namespaces
using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using ArangoDBNetStandard.DocumentApi.Models;

// Your helper + model namespaces
using SmartTour.Data;         // for ArangoDBHelper
using SmartTour.Models;  // for Place

namespace SmartTour.Services
{
    public class PlaceService
    {
        private readonly ArangoDBHelper _helper;

        public PlaceService(ArangoDBHelper helper)
        {
            _helper = helper;
        }

        /// <summary>
        /// 1. List all places (List<Place>).
        /// </summary>
        public async Task<List<Place>> GetAllAsync()
        {
            // AQL: return every document in Places
            var aql = "FOR p IN Places RETURN p";
            var cursor = await _helper.Client.Cursor.PostCursorAsync<Place>(
                new PostCursorBody { Query = aql }
            );
            return cursor.Result.ToList(); // Result is List<Place>
        }

        /// <summary>
        /// 2. Get one place by its key (_key). Returns null if not found or any error.
        /// </summary>
        public async Task<Place> GetByKeyAsync(string key)
        {
            try
            {
                // GetDocumentAsync<Place> now returns Place directly
                var place = await _helper.Client.Document.GetDocumentAsync<Place>(
                    "Places",  // collection name
                    key        // document _key
                );
                return place;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 3. Insert a new place. Returns the new document’s _id ("Places/<key>").
        /// </summary>
        public async Task<string> CreateAsync(Place place)
        {
            var response = await _helper.Client.Document.PostDocumentAsync<Place>(
                "Places", // collection name
                place     // POCO to serialize
            );
            return response._id;
        }

        /// <summary>
        /// 4. Replace an existing place document (full replace). Returns true on success.
        /// </summary>
        public async Task<bool> UpdateAsync(string key, Place updated)
        {
            try
            {
                await _helper.Client.Document.PutDocumentAsync<Place>(
                    "Places",  // collection name
                    key,       // document _key
                    updated    // the full Place object
                );
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 5. Delete a place by its _key. Returns true on success.
        /// </summary>
        public async Task<bool> DeleteAsync(string key)
        {
            try
            {
                await _helper.Client.Document.DeleteDocumentAsync(
                    "Places", // collection name
                    key
                );
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 6. (Optional) Search places by tag. Returns all places where the given tag appears in their tags array.
        /// </summary>
        public async Task<List<Place>> SearchByTagAsync(string tag)
        {
            var aql = @"
                FOR p IN Places
                  FILTER @tag IN p.tags
                  RETURN p
            ";
            var cursor = await _helper.Client.Cursor.PostCursorAsync<Place>(
                new PostCursorBody
                {
                    Query    = aql,
                    BindVars = new Dictionary<string, object> { { "tag", tag } }
                }
            );
            return cursor.Result.ToList();
        }
    }
}