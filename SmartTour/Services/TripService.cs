// File: SmartTour/Data/Services/TripService.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ArangoDB driver namespaces
using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using ArangoDBNetStandard.DocumentApi.Models;

// Your helper + model namespaces
using SmartTour.Data;         // for ArangoDBHelper
using SmartTour.Models;  // for Trip

namespace SmartTour.Services
{
    public class TripService
    {
        private readonly ArangoDBHelper _helper;

        public TripService(ArangoDBHelper helper)
        {
            _helper = helper;
        }

        /// <summary>
        /// 1. List all trips. Returns a List<Trip>.
        /// </summary>
        public async Task<List<Trip>> GetAllAsync()
        {
            // AQL to get every document from Trips
            var aql = "FOR t IN Trips RETURN t";
            var cursor = await _helper.Client.Cursor.PostCursorAsync<Trip>(
                new PostCursorBody { Query = aql }
            );
            return cursor.Result.ToList(); // Result is List<Trip>
        }

        /// <summary>
        /// 2. Get one trip by its _key. Returns null if not found or error.
        /// </summary>
        public async Task<Trip> GetByKeyAsync(string key)
        {
            try
            {
                // In your driver version, GetDocumentAsync<Trip> returns Trip directly
                var trip = await _helper.Client.Document.GetDocumentAsync<Trip>(
                    "Trips", // collection name
                    key      // document _key
                );
                return trip;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 3. Create a new trip document. Returns the new document's _id (e.g. "Trips/trip1").
        /// </summary>
        public async Task<string> CreateAsync(Trip trip)
        {
            var response = await _helper.Client.Document.PostDocumentAsync<Trip>(
                "Trips", // collection name
                trip     // POCO will be serialized to JSON
            );
            return response._id;
        }

        /// <summary>
        /// 4. Replace an existing trip (full document replace). Returns true on success.
        /// </summary>
        public async Task<bool> UpdateAsync(string key, Trip updated)
        {
            try
            {
                // PutDocumentAsync<Trip>(collectionName, documentKey, updatedObject)
                await _helper.Client.Document.PutDocumentAsync<Trip>(
                    "Trips",  // collection name
                    key,      // document _key
                    updated   // the entire Trip object to store
                );
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 5. Delete a trip by its _key. Returns true on success.
        /// </summary>
        public async Task<bool> DeleteAsync(string key)
        {
            try
            {
                await _helper.Client.Document.DeleteDocumentAsync(
                    "Trips", // collection name
                    key      // document _key
                );
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Trip> GetTripAsync(string key)
        {
            var aql = @"
                RETURN DOCUMENT(CONCAT('Trips/', @key))
            ";

            var cursor = await _helper.Client.Cursor.PostCursorAsync<Trip>(
                new PostCursorBody
                {
                    Query = aql,
                    BindVars = new Dictionary<string, object> { { "key", key } }
                }
            );

            return cursor.Result.FirstOrDefault();
        }

        public async Task<IEnumerable<Trip>> GetUpcomingTripsAsync(string userKey)
        {
            var aql = @"
                FOR t IN Trips
                    FILTER t.user_key == @userKey
                    AND DATE_TIMESTAMP(t.start_date) >= DATE_TIMESTAMP(@today)
                    SORT t.start_date ASC
                    RETURN t
            ";

            var cursor = await _helper.Client.Cursor.PostCursorAsync<Trip>(
                new PostCursorBody
                {
                    Query = aql,
                    BindVars = new Dictionary<string, object>
                    {
                        { "userKey", userKey },
                        { "today", DateTime.UtcNow.Date }
                    }
                }
            );

            return cursor.Result;
        }

        public async Task<IEnumerable<Trip>> GetPastTripsAsync(string userKey)
        {
            var aql = @"
                FOR t IN Trips
                    FILTER t.user_key == @userKey
                    AND DATE_TIMESTAMP(t.end_date) < DATE_TIMESTAMP(@today)
                    SORT t.end_date DESC
                    RETURN t
            ";

            var cursor = await _helper.Client.Cursor.PostCursorAsync<Trip>(
                new PostCursorBody
                {
                    Query = aql,
                    BindVars = new Dictionary<string, object>
                    {
                        { "userKey", userKey },
                        { "today", DateTime.UtcNow.Date }
                    }
                }
            );

            return cursor.Result;
        }

        public async Task<bool> UpdateTripAsync(Trip trip)
        {
            try
            {
                var response = await _helper.Client.Document.PutDocumentAsync(
                    "Trips",
                    trip.Key,
                    trip
                );
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteTripAsync(string key)
        {
            try
            {
                await _helper.Client.Document.DeleteDocumentAsync("Trips", key);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> CreateTripAsync(Trip trip)
        {
            var response = await _helper.Client.Document.PostDocumentAsync(
                "Trips",
                trip
            );
            return response._key;
        }
    }
}