using System;
using System.Linq;
using System.Threading.Tasks;

// Core ArangoDB client types
using ArangoDBNetStandard;

// HTTP transport factory (Basic Auth)
using ArangoDBNetStandard.Transport.Http;

// Collection‐related models (PostCollectionBody, CollectionType, etc.)
using ArangoDBNetStandard.CollectionApi.Models;

// Models for the “GetCollections” response
using ArangoDBNetStandard.CollectionApi.Models;

// (Note: No more CreateCollectionBody, no more CollectionConflictException)
using Microsoft.Extensions.Configuration;
using SmartTour.Models;

namespace SmartTour.Data
{
    public class ArangoDBHelper
    {
        private readonly ArangoDBClient _arango;

        public ArangoDBHelper(IConfiguration configuration)
        {
            // 1) Read settings from appsettings.json → "Arango" section
            var section  = configuration.GetSection("Arango");
            var url      = section.GetValue<string>("Url");       // e.g. "http://localhost:8529/"
            var database = section.GetValue<string>("Database");  // e.g. "SmartTourDB"
            var username = section.GetValue<string>("Username");  // e.g. "root"
            var password = section.GetValue<string>("Password");  // e.g. "yourRootPassword"

            // 2) Build a HttpApiTransport using Basic Auth
            var transport = HttpApiTransport.UsingBasicAuth(
                new Uri(url),
                database,
                username,
                password
            );

            // 3) Instantiate the ArangoDBClient pointed at that database
            _arango = new ArangoDBClient(transport);
        }

        /// <summary>
        /// Ensures that the five collections (3 document + 2 edge) exist:
        ///   • Users         (Document)
        ///   • Places        (Document)
        ///   • Trips         (Document)
        ///   • Visited       (Edge)
        ///   • RelatedPlaces (Edge)
        /// 
        /// If any collection is missing, it will be created automatically.
        /// </summary>
        public async Task EnsureCollectionsAsync()
        {
            // Document‐type collections
            await CreateDocumentCollectionIfNotExistsAsync("Users");
            await CreateDocumentCollectionIfNotExistsAsync("Places");
            await CreateDocumentCollectionIfNotExistsAsync("Trips");

            // Edge‐type collections
            await CreateEdgeCollectionIfNotExistsAsync("Visited");
            await CreateEdgeCollectionIfNotExistsAsync("RelatedPlaces");
        }

        /// <summary>
        /// Checks for an existing document collection named <paramref name="name"/>.
        /// If not found, creates a new document collection.
        /// </summary>
        private async Task CreateDocumentCollectionIfNotExistsAsync(string name)
        {
            // 1) List all collections in the current database
            var allCollectionsResponse = await _arango.Collection.GetCollectionsAsync();
            var collectionsList = allCollectionsResponse.Result;

            // 2) Check if `name` is already present
            bool found = collectionsList.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

            if (found)
            {
                Console.WriteLine($"[ArangoDBHelper] Document collection already exists: {name}");
                return;
            }

            // 3) If not found, create it
            try
            {
                var body = new PostCollectionBody
                {
                    Name = name,
                    Type = CollectionType.Document
                };

                await _arango.Collection.PostCollectionAsync(body);
                Console.WriteLine($"[ArangoDBHelper] Created document collection: {name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ArangoDBHelper] Error creating document collection '{name}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Checks for an existing edge collection named <paramref name="name"/>.
        /// If not found, creates a new edge collection.
        /// </summary>
        private async Task CreateEdgeCollectionIfNotExistsAsync(string name)
        {
            // 1) List all collections in the current database
            var allCollectionsResponse = await _arango.Collection.GetCollectionsAsync();
            var collectionsList = allCollectionsResponse.Result;

            // 2) Check if `name` is already present
            bool found = collectionsList.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

            if (found)
            {
                Console.WriteLine($"[ArangoDBHelper] Edge collection already exists: {name}");
                return;
            }

            // 3) If not found, create it as an edge collection
            try
            {
                var body = new PostCollectionBody
                {
                    Name = name,
                    Type = CollectionType.Edge
                };

                await _arango.Collection.PostCollectionAsync(body);
                Console.WriteLine($"[ArangoDBHelper] Created edge collection: {name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ArangoDBHelper] Error creating edge collection '{name}': {ex.Message}");
                throw;
            }
        }
        /// <summary>
        /// Inserts a new User document into the "Users" collection.
        /// Returns the full ArangoDB _id (e.g. "Users/alice").
        /// </summary>
        public async Task<string> InsertUserAsync(User user)
        {
            // If user.Key is null or empty, ArangoDB will auto-generate a key.
            var response = await _arango.Document.PostDocumentAsync<User>(
                collectionName: "Users",
                user
            );
            // response._id looks like "Users/<key>"
            return response._id;
        }

        /// <summary>
        /// Inserts a new Place document into the "Places" collection.
        /// Returns the full ArangoDB _id (e.g. "Places/paris").
        /// </summary>
        public async Task<string> InsertPlaceAsync(Place place)
        {
            var response = await _arango.Document.PostDocumentAsync<Place>(
                collectionName: "Places",
                place
            );
            return response._id;
        }

        /// <summary>
        /// Inserts a new Trip document into the "Trips" collection.
        /// Returns the full ArangoDB _id (e.g. "Trips/trip1").
        /// </summary>
        public async Task<string> InsertTripAsync(Trip trip)
        {
            var response = await _arango.Document.PostDocumentAsync<Trip>(
                collectionName: "Trips",
               trip
            );
            return response._id;
        }

        /// <summary>
        /// Inserts a new edge into the "Visited" edge collection.
        /// The "From" and "To" fields must be fully-qualified (_from = "Users/alice", _to = "Places/paris").
        /// Returns the full ArangoDB _id of the edge (e.g. "Visited/12345abcdef").
        /// </summary>
        public async Task<string> InsertVisitedEdgeAsync(VisitedEdge visited)
        {
            var response = await _arango.Document.PostDocumentAsync<VisitedEdge>(
                collectionName: "Visited",
                visited
            );
            return response._id;
        }

        /// <summary>
        /// Inserts a new edge into the "RelatedPlaces" edge collection.
        /// </summary>
        public async Task<string> InsertRelatedPlaceEdgeAsync(RelatedPlacesEdge edge)
        {
            
            var response = await _arango.Document.PostDocumentAsync<RelatedPlacesEdge>(
                collectionName: "RelatedPlaces",
                edge
            );
            return response._id;
        }

        /// <summary>
        /// Exposes the underlying ArangoDBClient so other services/controllers can run AQL, 
        /// insert documents, create edges, etc.
        /// </summary>
        public ArangoDBClient Client => _arango;
    }
}