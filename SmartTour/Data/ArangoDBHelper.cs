using System;
using System.Linq;
using System.Threading.Tasks;

// Core ArangoDB client types
using ArangoDBNetStandard;

// HTTP transport factory (Basic Auth)
using ArangoDBNetStandard.Transport.Http;

// Collection‐related models (PostCollectionBody, CollectionType, etc.)
using ArangoDBNetStandard.CollectionApi.Models;

// Models for the "GetCollections" response
using ArangoDBNetStandard.CollectionApi.Models;

// (Note: No more CreateCollectionBody, no more CollectionConflictException)
using Microsoft.Extensions.Configuration;
using SmartTour.Models;
using System.Collections.Generic;
using ArangoDBNetStandard.CursorApi.Models;

namespace SmartTour.Data
{
    public class ArangoDBHelper
    {
        private readonly ArangoDBClient _arango;
        private readonly IConfiguration _configuration;

        public ArangoDBHelper(IConfiguration configuration)
        {
            _configuration = configuration;
            
            // 1) Read settings from appsettings.json → "Arango" section
            var section  = configuration.GetSection("Arango");
            var url      = section.GetValue<string>("Url");       // e.g. "http://localhost:8529/"
            var database = section.GetValue<string>("Database");  // e.g. "SmartTourDB"
            var username = section.GetValue<string>("Username");  // e.g. "root"
            var password = section.GetValue<string>("Password");  // e.g. "yourRootPassword"

            Console.WriteLine($"[ArangoDBHelper] Initializing with URL: {url}, Database: {database}");

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
        /// Tests the connection to ArangoDB
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                Console.WriteLine("[ArangoDBHelper] Testing database connection...");
                var response = await _arango.Database.GetCurrentDatabaseInfoAsync();
                Console.WriteLine($"[ArangoDBHelper] Successfully connected to database: {response.Result.Name}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ArangoDBHelper] Database connection failed: {ex.Message}");
                Console.WriteLine($"[ArangoDBHelper] Stack trace: {ex.StackTrace}");
                return false;
            }
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
            try
            {
                Console.WriteLine("[ArangoDBHelper] Testing connection before creating collections...");
                if (!await TestConnectionAsync())
                {
                    throw new Exception("Could not connect to the database. Please check your connection settings.");
                }

                // Document‐type collections
                await CreateDocumentCollectionIfNotExistsAsync("Users");
                await CreateDocumentCollectionIfNotExistsAsync("Places");
                await CreateDocumentCollectionIfNotExistsAsync("Trips");

                // Edge‐type collections
                await CreateEdgeCollectionIfNotExistsAsync("Visited");
                await CreateEdgeCollectionIfNotExistsAsync("RelatedPlaces");

                // Initialize sample data
                await InitializeSampleDataAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ArangoDBHelper] Error ensuring collections: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initializes sample data in the database
        /// </summary>
        /// <param name="force">If true, will clear existing data before initializing</param>
        public async Task InitializeSampleDataAsync(bool force = false)
        {
            try
            {
                Console.WriteLine("[ArangoDBHelper] Starting sample data initialization...");
                
                // Check if we already have data
                var aql = "RETURN LENGTH(FOR p IN Places RETURN 1)";
                var cursor = await _arango.Cursor.PostCursorAsync<int>(
                    new PostCursorBody { Query = aql }
                );
                
                var count = cursor.Result.First();
                Console.WriteLine($"[ArangoDBHelper] Found {count} existing places");
                
                if (count > 0)
                {
                    if (!force)
                    {
                        Console.WriteLine("[ArangoDBHelper] Data already exists, skipping initialization");
                        return;
                    }
                    
                    Console.WriteLine("[ArangoDBHelper] Force flag is set, clearing existing data...");
                    await ClearExistingDataAsync();
                }

                // Sample places with their tags
                var places = new List<Place>
                {
                    new Place
                    {
                        Key = "eiffel_tower",
                        Name = "Eiffel Tower",
                        City = "Paris",
                        Country = "France",
                        Type = "Landmark",
                        Tags = new List<string> { "landmark", "romantic", "history", "architecture", "views" },
                        Description = "The iconic iron lattice tower on the Champ de Mars in Paris."
                    },
                    new Place
                    {
                        Key = "louvre_museum",
                        Name = "Louvre Museum",
                        City = "Paris",
                        Country = "France",
                        Type = "Museum",
                        Tags = new List<string> { "art", "history", "culture", "museum", "indoor" },
                        Description = "The world's largest art museum and a historic monument in Paris."
                    },
                    new Place
                    {
                        Key = "notre_dame",
                        Name = "Notre-Dame Cathedral",
                        City = "Paris",
                        Country = "France",
                        Type = "Religious Site",
                        Tags = new List<string> { "architecture", "history", "religious", "culture" },
                        Description = "A medieval Catholic cathedral on the Île de la Cité in Paris."
                    },
                    new Place
                    {
                        Key = "times_square",
                        Name = "Times Square",
                        City = "New York",
                        Country = "USA",
                        Type = "Plaza",
                        Tags = new List<string> { "urban", "shopping", "entertainment", "nightlife" },
                        Description = "A major commercial intersection and tourist destination in New York City."
                    },
                    new Place
                    {
                        Key = "central_park",
                        Name = "Central Park",
                        City = "New York",
                        Country = "USA",
                        Type = "Park",
                        Tags = new List<string> { "nature", "outdoor", "relaxation", "recreation" },
                        Description = "An urban oasis in the heart of New York City."
                    },
                    new Place
                    {
                        Key = "statue_liberty",
                        Name = "Statue of Liberty",
                        City = "New York",
                        Country = "USA",
                        Type = "Landmark",
                        Tags = new List<string> { "landmark", "history", "views", "outdoor" },
                        Description = "A colossal neoclassical sculpture on Liberty Island in New York Harbor."
                    },
                    new Place
                    {
                        Key = "sensoji_temple",
                        Name = "Sensoji Temple",
                        City = "Tokyo",
                        Country = "Japan",
                        Type = "Religious Site",
                        Tags = new List<string> { "culture", "religious", "history", "architecture" },
                        Description = "An ancient Buddhist temple in Asakusa, Tokyo."
                    },
                    new Place
                    {
                        Key = "shibuya_crossing",
                        Name = "Shibuya Crossing",
                        City = "Tokyo",
                        Country = "Japan",
                        Type = "Landmark",
                        Tags = new List<string> { "urban", "shopping", "nightlife", "culture" },
                        Description = "The world's busiest pedestrian crossing in Tokyo."
                    },
                    new Place
                    {
                        Key = "tokyo_tower",
                        Name = "Tokyo Tower",
                        City = "Tokyo",
                        Country = "Japan",
                        Type = "Landmark",
                        Tags = new List<string> { "landmark", "views", "architecture", "nightlife" },
                        Description = "A communications and observation tower in the Shiba-koen district of Tokyo."
                    }
                };

                Console.WriteLine($"[ArangoDBHelper] Inserting {places.Count} places...");

                // Insert all places
                foreach (var place in places)
                {
                    try
                    {
                        await InsertPlaceAsync(place);
                        Console.WriteLine($"[ArangoDBHelper] Inserted place: {place.Name}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ArangoDBHelper] Error inserting place {place.Name}: {ex.Message}");
                        throw;
                    }
                }

                Console.WriteLine("[ArangoDBHelper] Creating relationships between places...");

                // Create relationships between places in the same city
                foreach (var city in places.Select(p => p.City).Distinct())
                {
                    var cityPlaces = places.Where(p => p.City == city).ToList();
                    Console.WriteLine($"[ArangoDBHelper] Creating relationships for {city} ({cityPlaces.Count} places)");
                    
                    for (int i = 0; i < cityPlaces.Count; i++)
                    {
                        for (int j = i + 1; j < cityPlaces.Count; j++)
                        {
                            try
                            {
                                var edge = new RelatedPlacesEdge
                                {
                                    From = $"Places/{cityPlaces[i].Key}",
                                    To = $"Places/{cityPlaces[j].Key}",
                                    Weight = 1
                                };
                                await InsertRelatedPlaceEdgeAsync(edge);
                                Console.WriteLine($"[ArangoDBHelper] Created relationship: {cityPlaces[i].Name} -> {cityPlaces[j].Name}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ArangoDBHelper] Error creating relationship between {cityPlaces[i].Name} and {cityPlaces[j].Name}: {ex.Message}");
                                throw;
                            }
                        }
                    }
                }

                Console.WriteLine("[ArangoDBHelper] Sample data initialization completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ArangoDBHelper] Error during sample data initialization: {ex.Message}");
                Console.WriteLine($"[ArangoDBHelper] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Clears all existing data from the collections
        /// </summary>
        private async Task ClearExistingDataAsync()
        {
            try
            {
                // Clear edges first to maintain referential integrity
                Console.WriteLine("[ArangoDBHelper] Clearing RelatedPlaces edges...");
                await TruncateCollectionAsync("RelatedPlaces");
                
                Console.WriteLine("[ArangoDBHelper] Clearing Visited edges...");
                await TruncateCollectionAsync("Visited");
                
                // Then clear documents
                Console.WriteLine("[ArangoDBHelper] Clearing Places...");
                await TruncateCollectionAsync("Places");
                
                Console.WriteLine("[ArangoDBHelper] Clearing Users...");
                await TruncateCollectionAsync("Users");
                
                Console.WriteLine("[ArangoDBHelper] Clearing Trips...");
                await TruncateCollectionAsync("Trips");
                
                Console.WriteLine("[ArangoDBHelper] All collections cleared successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ArangoDBHelper] Error clearing existing data: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Truncates (removes all documents from) a collection
        /// </summary>
        private async Task TruncateCollectionAsync(string collectionName)
        {
            try
            {
                await _arango.Collection.TruncateCollectionAsync(collectionName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ArangoDBHelper] Error truncating collection {collectionName}: {ex.Message}");
                throw;
            }
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
            var response = await _arango.Document.PostDocumentAsync<User>(
                collectionName: "Users",
                user
            );
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