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
        ///   • TripSuggestions (Document)
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
                await CreateDocumentCollectionIfNotExistsAsync("TripSuggestions");

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
                    // Europe
                    new Place
                    {
                        Key = "eiffel_tower",
                        Name = "Eiffel Tower",
                        City = "Paris",
                        Country = "France",
                        Continent = "Europe",
                        Region = "Western Europe",
                        Type = "Landmark",
                        Tags = new List<string> { "landmark", "romantic", "history", "architecture", "views", "photography" },
                        Description = "The iconic iron lattice tower on the Champ de Mars in Paris.",
                        ImageUrl = "https://images.unsplash.com/photo-1543349689-9a4d426bee8e?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "louvre_museum",
                        Name = "Louvre Museum",
                        City = "Paris",
                        Country = "France",
                        Continent = "Europe",
                        Region = "Western Europe",
                        Type = "Museum",
                        Tags = new List<string> { "art", "history", "culture", "museum", "indoor", "architecture" },
                        Description = "The world's largest art museum and a historic monument in Paris.",
                        ImageUrl = "https://images.unsplash.com/photo-1544413660-299165566b1d?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "versailles",
                        Name = "Palace of Versailles",
                        City = "Versailles",
                        Country = "France",
                        Continent = "Europe",
                        Region = "Western Europe",
                        Type = "Palace",
                        Tags = new List<string> { "history", "architecture", "art", "garden", "luxury" },
                        Description = "The principal royal residence of France from 1682 until the start of the French Revolution.",
                        ImageUrl = "https://images.unsplash.com/photo-1597584056824-f708d7438a82?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "colosseum",
                        Name = "Colosseum",
                        City = "Rome",
                        Country = "Italy",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Historic Site",
                        Tags = new List<string> { "history", "architecture", "ancient", "landmark", "culture" },
                        Description = "An ancient amphitheater in the center of Rome, symbol of the Roman Empire.",
                        ImageUrl = "https://images.unsplash.com/photo-1552832230-c0197dd311b5?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "vatican_museums",
                        Name = "Vatican Museums",
                        City = "Rome",
                        Country = "Italy",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Museum",
                        Tags = new List<string> { "art", "history", "culture", "religious", "indoor" },
                        Description = "The public museums of the Vatican City, displaying works from the Roman Catholic Church's collection.",
                        ImageUrl = "https://images.unsplash.com/photo-1590856029826-c7a73142bbf1?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "sagrada_familia",
                        Name = "Sagrada Familia",
                        City = "Barcelona",
                        Country = "Spain",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Religious Site",
                        Tags = new List<string> { "architecture", "religious", "art", "landmark", "culture" },
                        Description = "A large unfinished Roman Catholic church designed by Antoni Gaudí.",
                        ImageUrl = "https://images.unsplash.com/photo-1583779457094-ab6f77f7bf57?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "park_guell",
                        Name = "Park Güell",
                        City = "Barcelona",
                        Country = "Spain",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Park",
                        Tags = new List<string> { "art", "architecture", "nature", "views", "outdoor" },
                        Description = "A public park system composed of gardens and architectural elements by Antoni Gaudí.",
                        ImageUrl = "https://images.unsplash.com/photo-1617104678098-de229db51175?q=80&w=2940&auto=format&fit=crop"
                    },

                    // Asia
                    new Place
                    {
                        Key = "sensoji_temple",
                        Name = "Sensoji Temple",
                        City = "Tokyo",
                        Country = "Japan",
                        Continent = "Asia",
                        Region = "Eastern Asia",
                        Type = "Religious Site",
                        Tags = new List<string> { "culture", "religious", "history", "architecture", "shopping" },
                        Description = "An ancient Buddhist temple in Asakusa, Tokyo's oldest temple.",
                        ImageUrl = "https://images.unsplash.com/photo-1570459027562-4a916cc6113f?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "shibuya_crossing",
                        Name = "Shibuya Crossing",
                        City = "Tokyo",
                        Country = "Japan",
                        Continent = "Asia",
                        Region = "Eastern Asia",
                        Type = "Landmark",
                        Tags = new List<string> { "urban", "shopping", "nightlife", "culture", "photography" },
                        Description = "The world's busiest pedestrian crossing, surrounded by tall buildings and neon signs.",
                        ImageUrl = "https://images.unsplash.com/photo-1542931287-023b922fa89b?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "meiji_shrine",
                        Name = "Meiji Shrine",
                        City = "Tokyo",
                        Country = "Japan",
                        Continent = "Asia",
                        Region = "Eastern Asia",
                        Type = "Religious Site",
                        Tags = new List<string> { "religious", "culture", "nature", "history", "peaceful" },
                        Description = "A Shinto shrine dedicated to Emperor Meiji and Empress Shoken.",
                        ImageUrl = "https://images.unsplash.com/photo-1583084647979-b53fbbc15e79?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "great_wall",
                        Name = "Great Wall of China",
                        City = "Beijing",
                        Country = "China",
                        Continent = "Asia",
                        Region = "Eastern Asia",
                        Type = "Historic Site",
                        Tags = new List<string> { "history", "architecture", "hiking", "views", "culture" },
                        Description = "An ancient series of walls and fortifications, one of the most famous landmarks in the world.",
                        ImageUrl = "https://images.unsplash.com/photo-1508804185872-d7badad00f7d?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "forbidden_city",
                        Name = "Forbidden City",
                        City = "Beijing",
                        Country = "China",
                        Continent = "Asia",
                        Region = "Eastern Asia",
                        Type = "Palace",
                        Tags = new List<string> { "history", "architecture", "culture", "art", "museum" },
                        Description = "A vast palace complex in central Beijing, now housing the Palace Museum.",
                        ImageUrl = "https://images.unsplash.com/photo-1584646098378-0874589d76b1?q=80&w=2940&auto=format&fit=crop"
                    },

                    // Americas
                    new Place
                    {
                        Key = "times_square",
                        Name = "Times Square",
                        City = "New York",
                        Country = "USA",
                        Continent = "North America",
                        Region = "Eastern United States",
                        Type = "Plaza",
                        Tags = new List<string> { "urban", "shopping", "entertainment", "nightlife", "culture" },
                        Description = "A major commercial intersection and tourist destination in New York City.",
                        ImageUrl = "https://images.unsplash.com/photo-1535964093104-5cc759d7dd85?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "central_park",
                        Name = "Central Park",
                        City = "New York",
                        Country = "USA",
                        Continent = "North America",
                        Region = "Eastern United States",
                        Type = "Park",
                        Tags = new List<string> { "nature", "outdoor", "relaxation", "recreation", "sports" },
                        Description = "An urban oasis in the heart of New York City.",
                        ImageUrl = "https://images.unsplash.com/photo-1568515387631-8b650bbcdb90?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "statue_liberty",
                        Name = "Statue of Liberty",
                        City = "New York",
                        Country = "USA",
                        Continent = "North America",
                        Region = "Eastern United States",
                        Type = "Landmark",
                        Tags = new List<string> { "landmark", "history", "views", "outdoor", "photography" },
                        Description = "A colossal neoclassical sculpture on Liberty Island in New York Harbor.",
                        ImageUrl = "https://images.unsplash.com/photo-1605130284535-11dd9eedc58a?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "golden_gate",
                        Name = "Golden Gate Bridge",
                        City = "San Francisco",
                        Country = "USA",
                        Continent = "North America",
                        Region = "Western United States",
                        Type = "Landmark",
                        Tags = new List<string> { "landmark", "architecture", "views", "photography", "outdoor" },
                        Description = "An iconic suspension bridge spanning the Golden Gate strait.",
                        ImageUrl = "https://images.unsplash.com/photo-1501594907352-04cda38ebc29?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "alcatraz",
                        Name = "Alcatraz Island",
                        City = "San Francisco",
                        Country = "USA",
                        Continent = "North America",
                        Region = "Western United States",
                        Type = "Historic Site",
                        Tags = new List<string> { "history", "museum", "views", "outdoor", "culture" },
                        Description = "A former federal prison on an island in San Francisco Bay.",
                        ImageUrl = "https://images.unsplash.com/photo-1589409514187-c21d14df0d04?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "machu_picchu",
                        Name = "Machu Picchu",
                        City = "Cusco",
                        Country = "Peru",
                        Continent = "South America",
                        Region = "Andes",
                        Type = "Historic Site",
                        Tags = new List<string> { "history", "architecture", "hiking", "nature", "culture" },
                        Description = "An ancient Incan city set high in the Andes Mountains.",
                        ImageUrl = "https://images.unsplash.com/photo-1526392060635-9d6019884377?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "christ_redeemer",
                        Name = "Christ the Redeemer",
                        City = "Rio de Janeiro",
                        Country = "Brazil",
                        Continent = "South America",
                        Region = "Eastern South America",
                        Type = "Landmark",
                        Tags = new List<string> { "landmark", "religious", "views", "culture", "photography" },
                        Description = "An Art Deco statue of Jesus Christ atop Mount Corcovado.",
                        ImageUrl = "https://images.unsplash.com/photo-1593995863951-57c27e518295?q=80&w=2940&auto=format&fit=crop"
                    },
                    // Add more nature and outdoor destinations
                    new Place
                    {
                        Key = "yellowstone",
                        Name = "Yellowstone National Park",
                        City = "Wyoming",
                        Country = "USA",
                        Continent = "North America",
                        Region = "Western United States",
                        Type = "National Park",
                        Tags = new List<string> { "nature", "wildlife", "hiking", "outdoor", "geothermal" },
                        Description = "America's first national park, known for its wildlife and unique geothermal features.",
                        ImageUrl = "https://images.unsplash.com/photo-1576434795764-7e27901b7af1?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "grand_canyon",
                        Name = "Grand Canyon",
                        City = "Arizona",
                        Country = "USA",
                        Continent = "North America",
                        Region = "Southwestern United States",
                        Type = "National Park",
                        Tags = new List<string> { "nature", "hiking", "views", "outdoor", "geology" },
                        Description = "A natural wonder carved by the Colorado River.",
                        ImageUrl = "https://images.unsplash.com/photo-1615551043360-33de8b5f410c?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "banff",
                        Name = "Banff National Park",
                        City = "Alberta",
                        Country = "Canada",
                        Continent = "North America",
                        Region = "Western Canada",
                        Type = "National Park",
                        Tags = new List<string> { "nature", "mountains", "hiking", "lakes", "wildlife" },
                        Description = "Canada's oldest national park, featuring stunning mountain landscapes and glacial lakes.",
                        ImageUrl = "https://images.unsplash.com/photo-1609790259520-1cf5e97c2c81?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "swiss_alps",
                        Name = "Swiss Alps",
                        City = "Zermatt",
                        Country = "Switzerland",
                        Continent = "Europe",
                        Region = "Western Europe",
                        Type = "Mountain Range",
                        Tags = new List<string> { "mountains", "hiking", "skiing", "nature", "views" },
                        Description = "Iconic mountain range offering year-round outdoor activities.",
                        ImageUrl = "https://images.unsplash.com/photo-1531366936337-7c912516345c?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "amazon",
                        Name = "Amazon Rainforest",
                        City = "Manaus",
                        Country = "Brazil",
                        Continent = "South America",
                        Region = "Eastern South America",
                        Type = "Rainforest",
                        Tags = new List<string> { "nature", "wildlife", "rainforest", "adventure", "eco-tourism" },
                        Description = "The world's largest rainforest, home to incredible biodiversity.",
                        ImageUrl = "https://images.unsplash.com/photo-1516908205727-40afad9449a8?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "galapagos",
                        Name = "Galapagos Islands",
                        City = "Puerto Ayora",
                        Country = "Ecuador",
                        Continent = "South America",
                        Region = "Eastern South America",
                        Type = "Islands",
                        Tags = new List<string> { "wildlife", "nature", "islands", "eco-tourism", "marine-life" },
                        Description = "Volcanic islands known for their unique wildlife and Charles Darwin's research.",
                        ImageUrl = "https://images.unsplash.com/photo-1544551763-46a013bb70d5?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "serengeti",
                        Name = "Serengeti National Park",
                        City = "Arusha",
                        Country = "Tanzania",
                        Continent = "Africa",
                        Region = "Eastern Africa",
                        Type = "National Park",
                        Tags = new List<string> { "wildlife", "safari", "nature", "adventure", "photography" },
                        Description = "Famous for the annual wildebeest migration and diverse wildlife.",
                        ImageUrl = "https://images.unsplash.com/photo-1516426122078-c23e76319801?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "fjords",
                        Name = "Norwegian Fjords",
                        City = "Bergen",
                        Country = "Norway",
                        Continent = "Europe",
                        Region = "Northern Europe",
                        Type = "Natural Wonder",
                        Tags = new List<string> { "nature", "fjords", "hiking", "views", "cruise" },
                        Description = "Dramatic fjord landscapes carved by glacial erosion.",
                        ImageUrl = "https://images.unsplash.com/photo-1601439678777-b2b3c56fa627?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "great_barrier",
                        Name = "Great Barrier Reef",
                        City = "Cairns",
                        Country = "Australia",
                        Continent = "Australia",
                        Region = "North Australia",
                        Type = "Marine Park",
                        Tags = new List<string> { "marine-life", "nature", "diving", "snorkeling", "eco-tourism" },
                        Description = "The world's largest coral reef system.",
                        ImageUrl = "https://images.unsplash.com/photo-1582967788606-a171c1080cb0?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "mount_fuji",
                        Name = "Mount Fuji",
                        City = "Fujinomiya",
                        Country = "Japan",
                        Continent = "Asia",
                        Region = "Eastern Asia",
                        Type = "Mountain",
                        Tags = new List<string> { "mountain", "hiking", "nature", "culture", "views" },
                        Description = "Japan's highest mountain and an iconic symbol of the country.",
                        ImageUrl = "https://images.unsplash.com/photo-1570459027562-4a916cc6113f?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "petra",
                        Name = "Petra",
                        City = "Wadi Musa",
                        Country = "Jordan",
                        Continent = "Asia",
                        Region = "Western Asia",
                        Type = "Historic Site",
                        Tags = new List<string> { "history", "archaeology", "culture", "hiking", "architecture" },
                        Description = "Ancient city carved into rose-colored rock faces.",
                        ImageUrl = "https://images.unsplash.com/photo-1579606032821-4e6161c81bd3?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "santorini",
                        Name = "Santorini",
                        City = "Thira",
                        Country = "Greece",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Island",
                        Tags = new List<string> { "beaches", "views", "culture", "relaxation", "romantic" },
                        Description = "Beautiful island known for its white-washed buildings and sunsets.",
                        ImageUrl = "https://images.unsplash.com/photo-1613395877344-13d4a8e0d49e?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "angkor_wat",
                        Name = "Angkor Wat",
                        City = "Siem Reap",
                        Country = "Cambodia",
                        Continent = "Asia",
                        Region = "South Asia",
                        Type = "Temple Complex",
                        Tags = new List<string> { "history", "culture", "architecture", "temples", "archaeology" },
                        Description = "Massive temple complex showcasing Khmer architecture.",
                        ImageUrl = "https://images.unsplash.com/photo-1600820641018-29832a777b0f?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "iceland_ring",
                        Name = "Iceland Ring Road",
                        City = "Reykjavik",
                        Country = "Iceland",
                        Continent = "Europe",
                        Region = "Northern Europe",
                        Type = "Scenic Route",
                        Tags = new List<string> { "nature", "waterfalls", "volcanoes", "northern-lights", "adventure" },
                        Description = "Circular route showcasing Iceland's diverse landscapes.",
                        ImageUrl = "https://images.unsplash.com/photo-1504893524553-b855bce32c67?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "zhangjiajie",
                        Name = "Zhangjiajie National Forest Park",
                        City = "Zhangjiajie",
                        Country = "China",
                        Continent = "Asia",
                        Region = "Eastern Asia",
                        Type = "National Park",
                        Tags = new List<string> { "nature", "mountains", "hiking", "views", "photography" },
                        Description = "Inspiration for Avatar's floating mountains, featuring unique quartzite sandstone pillars.",
                        ImageUrl = "https://images.unsplash.com/photo-1513977055326-8ae6272d90a7?q=80&w=2940&auto=format&fit=crop"
                    },
                    // Additional diverse destinations
                    new Place
                    {
                        Key = "maldives",
                        Name = "Maldives Islands",
                        City = "Male",
                        Country = "Maldives",
                        Continent = "Asia",
                        Region = "Southern Asia",
                        Type = "Beach Paradise",
                        Tags = new List<string> { "beaches", "luxury", "marine-life", "relaxation", "romantic" },
                        Description = "Tropical paradise with overwater bungalows and crystal-clear waters.",
                        ImageUrl = "https://images.unsplash.com/photo-1514282401047-d79a71a590e8?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "bora_bora",
                        Name = "Bora Bora",
                        City = "Vaitape",
                        Country = "French Polynesia",
                        Continent = "Oceania",
                        Region = "Western Pacific",
                        Type = "Island",
                        Tags = new List<string> { "beaches", "luxury", "romantic", "marine-life", "relaxation" },
                        Description = "Stunning island surrounded by a turquoise lagoon and coral reefs.",
                        ImageUrl = "https://images.unsplash.com/photo-1598127895781-05ce6bd1e696?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "phi_phi",
                        Name = "Phi Phi Islands",
                        City = "Krabi",
                        Country = "Thailand",
                        Continent = "Asia",
                        Region = "South Asia",
                        Type = "Islands",
                        Tags = new List<string> { "beaches", "nature", "snorkeling", "adventure", "nightlife" },
                        Description = "Tropical islands with limestone cliffs and pristine beaches.",
                        ImageUrl = "https://images.unsplash.com/photo-1537956965359-7573183d1f57?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "mount_everest",
                        Name = "Mount Everest Base Camp",
                        City = "Solukhumbu",
                        Country = "Nepal",
                        Continent = "Asia",
                        Region = "Southern Asia",
                        Type = "Mountain",
                        Tags = new List<string> { "mountains", "hiking", "adventure", "nature", "extreme" },
                        Description = "The gateway to the world's highest peak.",
                        ImageUrl = "https://images.unsplash.com/photo-1486911278844-a81c5267e227?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "kilimanjaro",
                        Name = "Mount Kilimanjaro",
                        City = "Moshi",
                        Country = "Tanzania",
                        Continent = "Africa",
                        Region = "Eastern Africa",
                        Type = "Mountain",
                        Tags = new List<string> { "mountains", "hiking", "adventure", "nature", "africa" },
                        Description = "Africa's highest peak and the world's highest free-standing mountain.",
                        ImageUrl = "https://images.unsplash.com/photo-1589553416260-f586c8f1514f?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "inca_trail",
                        Name = "Inca Trail",
                        City = "Cusco",
                        Country = "Peru",
                        Continent = "South America",
                        Region = "Andes",
                        Type = "Historic Trail",
                        Tags = new List<string> { "hiking", "history", "mountains", "adventure", "archaeology" },
                        Description = "Ancient trail through the Andes leading to Machu Picchu.",
                        ImageUrl = "https://images.unsplash.com/photo-1587595431973-160d0d94add1?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "amalfi_coast",
                        Name = "Amalfi Coast",
                        City = "Amalfi",
                        Country = "Italy",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Coastal Region",
                        Tags = new List<string> { "coastal", "culture", "food", "romantic", "views" },
                        Description = "Dramatic coastline with colorful villages and Mediterranean charm.",
                        ImageUrl = "https://images.unsplash.com/photo-1633321088355-d0f81134ca3b?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "cinque_terre",
                        Name = "Cinque Terre",
                        City = "La Spezia",
                        Country = "Italy",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Coastal Villages",
                        Tags = new List<string> { "coastal", "hiking", "culture", "views", "food" },
                        Description = "Five colorful villages connected by scenic hiking trails.",
                        ImageUrl = "https://images.unsplash.com/photo-1498307833015-e7b400441eb8?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "cappadocia",
                        Name = "Cappadocia",
                        City = "Göreme",
                        Country = "Turkey",
                        Continent = "Asia",
                        Region = "Western Asia",
                        Type = "Historic Region",
                        Tags = new List<string> { "history", "culture", "adventure", "unique", "photography" },
                        Description = "Famous for its unique rock formations and hot air balloon rides.",
                        ImageUrl = "https://images.unsplash.com/photo-1641128324972-af3212f0f6bd?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "northern_lights",
                        Name = "Tromsø Northern Lights",
                        City = "Tromsø",
                        Country = "Norway",
                        Continent = "Europe",
                        Region = "Northern Europe",
                        Type = "Natural Phenomenon",
                        Tags = new List<string> { "nature", "northern-lights", "winter", "photography", "unique" },
                        Description = "Prime location for viewing the Aurora Borealis.",
                        ImageUrl = "https://images.unsplash.com/photo-1483347756197-71ef80e95f73?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "hawaii_volcanoes",
                        Name = "Hawaii Volcanoes National Park",
                        City = "Hawaii",
                        Country = "USA",
                        Continent = "North America",
                        Region = "Western United States",
                        Type = "National Park",
                        Tags = new List<string> { "volcanoes", "nature", "hiking", "unique", "geology" },
                        Description = "Active volcanoes and dramatic landscapes on the Big Island.",
                        ImageUrl = "https://images.unsplash.com/photo-1505244965963-d3d2d1b4d9c9?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "victoria_falls",
                        Name = "Victoria Falls",
                        City = "Livingstone",
                        Country = "Zambia",
                        Continent = "Africa",
                        Region = "Eastern Africa",
                        Type = "Waterfall",
                        Tags = new List<string> { "waterfall", "nature", "adventure", "africa", "wildlife" },
                        Description = "One of the world's largest waterfalls, known locally as 'The Smoke that Thunders'.",
                        ImageUrl = "https://images.unsplash.com/photo-1624821558130-b325d7946fc6?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "raja_ampat",
                        Name = "Raja Ampat Islands",
                        City = "West Papua",
                        Country = "Indonesia",
                        Continent = "Asia",
                        Region = "Eastern Asia",
                        Type = "Islands",
                        Tags = new List<string> { "diving", "marine-life", "nature", "beaches", "eco-tourism" },
                        Description = "Remote archipelago with incredible marine biodiversity.",
                        ImageUrl = "https://images.unsplash.com/photo-1516690561799-46d8f74f9abf?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "salar_uyuni",
                        Name = "Salar de Uyuni",
                        City = "Uyuni",
                        Country = "Bolivia",
                        Continent = "South America",
                        Region = "Western South America",
                        Type = "Salt Flat",
                        Tags = new List<string> { "unique", "nature", "photography", "adventure", "geology" },
                        Description = "World's largest salt flat, creating mirror-like surfaces.",
                        ImageUrl = "https://images.unsplash.com/photo-1547234935-80c7145ec969?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "antelope_canyon",
                        Name = "Antelope Canyon",
                        City = "Page",
                        Country = "USA",
                        Continent = "North America",
                        Region = "Southwestern United States",
                        Type = "Slot Canyon",
                        Tags = new List<string> { "nature", "photography", "geology", "unique", "native-american" },
                        Description = "Beautiful slot canyon with stunning light beams and rock formations.",
                        ImageUrl = "https://images.unsplash.com/photo-1444076784383-69ff7bae1b0a?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "plitvice_lakes",
                        Name = "Plitvice Lakes",
                        City = "Plitvička Jezera",
                        Country = "Croatia",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "National Park",
                        Tags = new List<string> { "nature", "waterfalls", "hiking", "lakes", "photography" },
                        Description = "Series of cascading lakes in stunning turquoise colors.",
                        ImageUrl = "https://images.unsplash.com/photo-1564324738080-bbbf8d6b4887?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "mount_cook",
                        Name = "Mount Cook National Park",
                        City = "Canterbury",
                        Country = "New Zealand",
                        Continent = "Australia",
                        Region = "New Zealand",
                        Type = "National Park",
                        Tags = new List<string> { "mountains", "hiking", "nature", "adventure", "photography" },
                        Description = "New Zealand's highest peak with stunning alpine scenery.",
                        ImageUrl = "https://images.unsplash.com/photo-1579868121087-74aa78352cc2?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "halong_bay",
                        Name = "Ha Long Bay",
                        City = "Ha Long",
                        Country = "Vietnam",
                        Continent = "Asia",
                        Region = "Eastern Asia",
                        Type = "Bay",
                        Tags = new List<string> { "nature", "cruise", "limestone", "culture", "photography" },
                        Description = "Thousands of limestone islands rising from emerald waters.",
                        ImageUrl = "https://images.unsplash.com/photo-1528127269322-539801943592?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "dolomites",
                        Name = "Dolomites",
                        City = "South Tyrol",
                        Country = "Italy",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Mountain Range",
                        Tags = new List<string> { "mountains", "hiking", "skiing", "nature", "photography" },
                        Description = "Dramatic mountain range with unique limestone formations.",
                        ImageUrl = "https://images.unsplash.com/photo-1601055283742-8b27e81b5553?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "matterhorn",
                        Name = "Matterhorn",
                        City = "Zermatt",
                        Country = "Switzerland",
                        Continent = "Europe",
                        Region = "Western Europe",
                        Type = "Mountain",
                        Tags = new List<string> { "mountains", "skiing", "hiking", "photography", "iconic" },
                        Description = "Iconic pyramid-shaped peak in the Alps.",
                        ImageUrl = "https://images.unsplash.com/photo-1565108160347-5498c67c4fcd?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "mykonos",
                        Name = "Mykonos",
                        City = "Mykonos",
                        Country = "Greece",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Island",
                        Tags = new List<string> { "beaches", "nightlife", "culture", "luxury", "architecture" },
                        Description = "Cosmopolitan island known for its vibrant nightlife, white-washed buildings, and beautiful beaches.",
                        ImageUrl = "https://images.unsplash.com/photo-1601581875309-fafbf2d3ed3a?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "meteora",
                        Name = "Meteora",
                        City = "Kalambaka",
                        Country = "Greece",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Historic Site",
                        Tags = new List<string> { "monasteries", "history", "nature", "unique", "photography" },
                        Description = "Monasteries perched atop dramatic rock formations, creating a unique spiritual landscape.",
                        ImageUrl = "https://images.unsplash.com/photo-1586755089284-d66d37498ef4?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "rhodes",
                        Name = "Rhodes Old Town",
                        City = "Rhodes",
                        Country = "Greece",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Historic Town",
                        Tags = new List<string> { "history", "medieval", "culture", "architecture", "beaches" },
                        Description = "Medieval walled city with stunning architecture and beautiful beaches nearby.",
                        ImageUrl = "https://images.unsplash.com/photo-1603565816030-6b389eeb23cb?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "delphi",
                        Name = "Delphi",
                        City = "Delphi",
                        Country = "Greece",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Archaeological Site",
                        Tags = new List<string> { "history", "ancient", "culture", "archaeology", "views" },
                        Description = "Ancient sanctuary and oracle, considered the 'navel of the world' by ancient Greeks.",
                        ImageUrl = "https://images.unsplash.com/photo-1558612899-669c9611c139?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "crete",
                        Name = "Crete",
                        City = "Heraklion",
                        Country = "Greece",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Island",
                        Tags = new List<string> { "history", "beaches", "food", "culture", "hiking" },
                        Description = "Largest Greek island featuring ancient ruins, mountain villages, and beautiful beaches.",
                        ImageUrl = "https://images.unsplash.com/photo-1586861635167-e5223aadc9fe?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "athens_acropolis",
                        Name = "Acropolis of Athens",
                        City = "Athens",
                        Country = "Greece",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Historic Site",
                        Tags = new List<string> { "history", "ancient", "architecture", "culture", "iconic" },
                        Description = "Ancient citadel containing the Parthenon and other significant ancient Greek buildings.",
                        ImageUrl = "https://images.unsplash.com/photo-1555993539-1732b0258235?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "naxos",
                        Name = "Naxos",
                        City = "Naxos",
                        Country = "Greece",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Island",
                        Tags = new List<string> { "beaches", "history", "food", "hiking", "relaxation" },
                        Description = "Largest Cycladic island with beautiful beaches, ancient ruins, and mountain villages.",
                        ImageUrl = "https://images.unsplash.com/photo-1586861635167-e5223aadc9fe?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "malta_valletta",
                        Name = "Valletta",
                        City = "Valletta",
                        Country = "Malta",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Historic City",
                        Tags = new List<string> { "history", "architecture", "culture", "mediterranean", "unesco" },
                        Description = "Historic fortress city with stunning baroque architecture and rich history.",
                        ImageUrl = "https://images.unsplash.com/photo-1514222134-b57cbb8ce073?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "dubrovnik",
                        Name = "Dubrovnik Old Town",
                        City = "Dubrovnik",
                        Country = "Croatia",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Historic City",
                        Tags = new List<string> { "history", "medieval", "coastal", "culture", "architecture" },
                        Description = "Pearl of the Adriatic with impressive medieval walls and crystal-clear waters.",
                        ImageUrl = "https://images.unsplash.com/photo-1555990538-c48aa0d12c87?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "cyprus_paphos",
                        Name = "Paphos",
                        City = "Paphos",
                        Country = "Cyprus",
                        Continent = "Asia",
                        Region = "Western Asia",
                        Type = "Historic Coastal City",
                        Tags = new List<string> { "history", "beaches", "archaeology", "culture", "mediterranean" },
                        Description = "Ancient harbor city with Roman ruins, beautiful beaches, and rich mythology.",
                        ImageUrl = "https://images.unsplash.com/photo-1529686342540-1b43aec0df75?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "corfu",
                        Name = "Corfu Old Town",
                        City = "Corfu",
                        Country = "Greece",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Historic Town",
                        Tags = new List<string> { "history", "architecture", "culture", "beaches", "food" },
                        Description = "Venetian-influenced town with stunning architecture and beautiful coastal views.",
                        ImageUrl = "https://images.unsplash.com/photo-1523537444585-432d3eb1c8a5?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "zakynthos",
                        Name = "Zakynthos",
                        City = "Zakynthos",
                        Country = "Greece",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Island",
                        Tags = new List<string> { "beaches", "nature", "photography", "swimming", "caves" },
                        Description = "Home to the famous Navagio Beach and Blue Caves, with crystal-clear waters.",
                        ImageUrl = "https://images.unsplash.com/photo-1523278669709-c05da80b6a65?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "milos",
                        Name = "Milos",
                        City = "Milos",
                        Country = "Greece",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Island",
                        Tags = new List<string> { "beaches", "geology", "photography", "swimming", "unique" },
                        Description = "Volcanic island known for its colorful rock formations and unique beach landscapes.",
                        ImageUrl = "https://images.unsplash.com/photo-1504109586057-7a2ae83d1338?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "split_croatia",
                        Name = "Split",
                        City = "Split",
                        Country = "Croatia",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Historic City",
                        Tags = new List<string> { "history", "roman", "beaches", "culture", "architecture" },
                        Description = "Roman palace turned living city with beautiful beaches and historic architecture.",
                        ImageUrl = "https://images.unsplash.com/photo-1555990538-32d1a3c15468?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "olympia",
                        Name = "Ancient Olympia",
                        City = "Olympia",
                        Country = "Greece",
                        Continent = "Europe",
                        Region = "Southern Europe",
                        Type = "Archaeological Site",
                        Tags = new List<string> { "history", "ancient", "sports", "culture", "archaeology" },
                        Description = "Birthplace of the Olympic Games and major Pan-Hellenic sanctuary.",
                        ImageUrl = "https://images.unsplash.com/photo-1533106958148-dadefcd4036a?q=80&w=2940&auto=format&fit=crop"
                    },
                    // East Asia
                    new Place
                    {
                        Key = "kyoto_temples",
                        Name = "Kyoto Temples",
                        City = "Kyoto",
                        Country = "Japan",
                        Continent = "Asia",
                        Region = "Eastern Asia",
                        Type = "Cultural Site",
                        Tags = new List<string> { "temples", "culture", "history", "gardens", "architecture" },
                        Description = "Ancient capital with over 1,600 Buddhist temples and beautiful gardens.",
                        ImageUrl = "https://images.unsplash.com/photo-1624253321171-1be53e12f5f4?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "seoul_palace",
                        Name = "Gyeongbokgung Palace",
                        City = "Seoul",
                        Country = "South Korea",
                        Continent = "Asia",
                        Region = "East Asia",
                        Type = "Palace",
                        Tags = new List<string> { "palace", "history", "culture", "architecture", "traditional" },
                        Description = "Main royal palace of the Joseon dynasty with traditional architecture and ceremonies.",
                        ImageUrl = "https://images.unsplash.com/photo-1578637387939-43c525550085?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "taipei_101",
                        Name = "Taipei 101",
                        City = "Taipei",
                        Country = "Taiwan",
                        Continent = "Asia",
                        Region = "East Asia",
                        Type = "Modern Landmark",
                        Tags = new List<string> { "modern", "architecture", "city", "views", "shopping" },
                        Description = "Iconic skyscraper offering panoramic views and modern architecture.",
                        ImageUrl = "https://images.unsplash.com/photo-1470004914212-05527e49370b?q=80&w=2940&auto=format&fit=crop"
                    },
                    // Oceania
                    new Place
                    {
                        Key = "uluru",
                        Name = "Uluru",
                        City = "Alice Springs",
                        Country = "Australia",
                        Continent = "Australia",
                        Region = "Central Australia",
                        Type = "Natural Monument",
                        Tags = new List<string> { "nature", "indigenous", "desert", "spiritual", "unique" },
                        Description = "Sacred Aboriginal site and massive sandstone monolith in the outback.",
                        ImageUrl = "https://images.unsplash.com/photo-1529516548873-9ce57c8f155e?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "milford_sound",
                        Name = "Milford Sound",
                        City = "Fiordland",
                        Country = "New Zealand",
                        Continent = "Australia",
                        Region = "New Zealand",
                        Type = "Fjord",
                        Tags = new List<string> { "nature", "fjords", "wildlife", "cruise", "photography" },
                        Description = "Stunning fjord with waterfalls, peaks, and diverse wildlife.",
                        ImageUrl = "https://images.unsplash.com/photo-1578862973944-aa3f689945a3?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "bora_bora_lagoon",
                        Name = "Bora Bora Lagoon",
                        City = "Bora Bora",
                        Country = "French Polynesia",
                        Continent = "Oceania",
                        Region = "Western Pacific",
                        Type = "Lagoon",
                        Tags = new List<string> { "beaches", "luxury", "marine-life", "romantic", "snorkeling" },
                        Description = "Crystal clear lagoon with overwater bungalows and coral gardens.",
                        ImageUrl = "https://images.unsplash.com/photo-1500930287596-c1ecaa373bb2?q=80&w=2940&auto=format&fit=crop"
                    },
                    // Americas
                    new Place
                    {
                        Key = "lake_louise",
                        Name = "Lake Louise",
                        City = "Banff",
                        Country = "Canada",
                        Continent = "North America",
                        Region = "Western Canada",
                        Type = "Lake",
                        Tags = new List<string> { "nature", "mountains", "hiking", "photography", "winter-sports" },
                        Description = "Turquoise lake surrounded by snow-capped peaks and glaciers.",
                        ImageUrl = "https://images.unsplash.com/photo-1501785888041-af3ef285b470?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "rio_carnival",
                        Name = "Rio Carnival",
                        City = "Rio de Janeiro",
                        Country = "Brazil",
                        Continent = "South America",
                        Region = "Eastern South America",
                        Type = "Cultural Event",
                        Tags = new List<string> { "culture", "festival", "music", "dance", "entertainment" },
                        Description = "World's largest carnival celebration with parades and samba dancing.",
                        ImageUrl = "https://images.unsplash.com/photo-1551464664-222eeb2d2034?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "easter_island",
                        Name = "Easter Island",
                        City = "Hanga Roa",
                        Country = "Chile",
                        Continent = "South America",
                        Region = "Eastern South America",
                        Type = "Archaeological Site",
                        Tags = new List<string> { "archaeology", "history", "culture", "unique", "mysterious" },
                        Description = "Remote island famous for its mysterious moai statues.",
                        ImageUrl = "https://images.unsplash.com/photo-1510711789248-087061cda288?q=80&w=2940&auto=format&fit=crop"
                    },
                    // Southeast Asia
                    new Place
                    {
                        Key = "bagan_temples",
                        Name = "Bagan Archaeological Zone",
                        City = "Bagan",
                        Country = "Myanmar",
                        Continent = "Asia",
                        Region = "South Asia",
                        Type = "Archaeological Site",
                        Tags = new List<string> { "temples", "history", "architecture", "sunrise", "photography" },
                        Description = "Ancient city with over 2,000 Buddhist temples and pagodas.",
                        ImageUrl = "https://images.unsplash.com/photo-1545759843-49d5f56cd73f?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "komodo_island",
                        Name = "Komodo National Park",
                        City = "Labuan Bajo",
                        Country = "Indonesia",
                        Continent = "Asia",
                        Region = "Eastern Asia",
                        Type = "National Park",
                        Tags = new List<string> { "wildlife", "nature", "dragons", "diving", "islands" },
                        Description = "Home to Komodo dragons, pristine beaches, and coral reefs.",
                        ImageUrl = "https://images.unsplash.com/photo-1589308454676-22a0e7be6069?q=80&w=2940&auto=format&fit=crop"
                    },
                    // Central Asia
                    new Place
                    {
                        Key = "registan",
                        Name = "Registan Square",
                        City = "Samarkand",
                        Country = "Uzbekistan",
                        Continent = "Asia",
                        Region = "Central Asia",
                        Type = "Historic Site",
                        Tags = new List<string> { "architecture", "history", "silk-road", "culture", "islamic" },
                        Description = "Heart of ancient Samarkand with stunning medieval Islamic architecture.",
                        ImageUrl = "https://images.unsplash.com/photo-1605147269139-c88ec2811d0a?q=80&w=2940&auto=format&fit=crop"
                    },
                    // Africa
                    new Place
                    {
                        Key = "sahara_morocco",
                        Name = "Sahara Desert",
                        City = "Merzouga",
                        Country = "Morocco",
                        Continent = "Africa",
                        Region = "Northern Africa",
                        Type = "Desert",
                        Tags = new List<string> { "desert", "adventure", "nature", "camping", "culture" },
                        Description = "Golden sand dunes and traditional Berber experiences in the Sahara.",
                        ImageUrl = "https://images.unsplash.com/photo-1548235890-693e7f112feb?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "madagascar_avenue",
                        Name = "Avenue of the Baobabs",
                        City = "Morondava",
                        Country = "Madagascar",
                        Continent = "Africa",
                        Region = "Eastern Africa",
                        Type = "Natural Site",
                        Tags = new List<string> { "nature", "unique", "photography", "trees", "sunset" },
                        Description = "Ancient baobab trees creating a stunning natural avenue.",
                        ImageUrl = "https://images.unsplash.com/photo-1548292463-c45fbf41bfd7?q=80&w=2940&auto=format&fit=crop"
                    },
                    // Additional East Asian Destinations
                    new Place
                    {
                        Key = "great_wall_mutianyu",
                        Name = "Great Wall at Mutianyu",
                        City = "Beijing",
                        Country = "China",
                        Continent = "Asia",
                        Region = "Eastern Asia",
                        Type = "Historic Site",
                        Tags = new List<string> { "history", "architecture", "hiking", "views", "iconic" },
                        Description = "Well-preserved section of the Great Wall with stunning mountain views.",
                        ImageUrl = "https://images.unsplash.com/photo-1508804185872-d7badad00f7d?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "osaka_castle",
                        Name = "Osaka Castle",
                        City = "Osaka",
                        Country = "Japan",
                        Continent = "Asia",
                        Region = "Eastern Asia",
                        Type = "Castle",
                        Tags = new List<string> { "history", "architecture", "culture", "gardens", "samurai" },
                        Description = "Historic castle showcasing Japanese architecture and samurai history.",
                        ImageUrl = "https://images.unsplash.com/photo-1590559899731-a382839e5549?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "jeju_island",
                        Name = "Jeju Island",
                        City = "Jeju",
                        Country = "South Korea",
                        Continent = "Asia",
                        Region = "Eastern Asia",
                        Type = "Island",
                        Tags = new List<string> { "nature", "beaches", "volcano", "culture", "hiking" },
                        Description = "Volcanic island with unique landscapes, beaches, and cultural sites.",
                        ImageUrl = "https://images.unsplash.com/photo-1588587789343-4ec849915380?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "terracotta_army",
                        Name = "Terracotta Army",
                        City = "Xi'an",
                        Country = "China",
                        Continent = "Asia",
                        Region = "Eastern Asia",
                        Type = "Archaeological Site",
                        Tags = new List<string> { "history", "archaeology", "culture", "art", "ancient" },
                        Description = "Ancient clay warrior army protecting Emperor Qin's tomb.",
                        ImageUrl = "https://images.unsplash.com/photo-1591709974243-c54abf29b428?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "ohrid_lake",
                        Name = "Lake Ohrid",
                        City = "Ohrid",
                        Country = "North Macedonia",
                        Continent = "Europe",
                        Region = "Southeastern Europe",
                        Type = "Natural and Cultural Site",
                        Tags = new List<string> { "lake", "unesco", "history", "culture", "nature", "beaches" },
                        Description = "One of Europe's oldest and deepest lakes, with medieval churches, monasteries, and ancient ruins along its shores.",
                        ImageUrl = "https://images.unsplash.com/photo-1592395688096-161bae60cd69?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "matka_canyon",
                        Name = "Matka Canyon",
                        City = "Skopje",
                        Country = "North Macedonia",
                        Continent = "Europe",
                        Region = "Southeastern Europe",
                        Type = "Natural Site",
                        Tags = new List<string> { "nature", "hiking", "caves", "monastery", "kayaking", "climbing" },
                        Description = "A stunning gorge with medieval monasteries, caves, and a lake perfect for outdoor activities.",
                        ImageUrl = "https://images.unsplash.com/photo-1608666634759-4376010f863d?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "old_bazaar_skopje",
                        Name = "Old Bazaar",
                        City = "Skopje",
                        Country = "North Macedonia",
                        Continent = "Europe",
                        Region = "Southeastern Europe",
                        Type = "Historic District",
                        Tags = new List<string> { "bazaar", "culture", "shopping", "history", "ottoman", "architecture" },
                        Description = "The largest bazaar in the Balkans outside Istanbul, featuring Ottoman architecture and traditional crafts.",
                        ImageUrl = "https://images.unsplash.com/photo-1566738273423-80d89874132a?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "samuel_fortress",
                        Name = "Samuel's Fortress",
                        City = "Ohrid",
                        Country = "North Macedonia",
                        Continent = "Europe",
                        Region = "Southeastern Europe",
                        Type = "Historic Site",
                        Tags = new List<string> { "fortress", "history", "views", "medieval", "architecture", "culture" },
                        Description = "Medieval fortress offering panoramic views of Lake Ohrid and the historic town.",
                        ImageUrl = "https://images.unsplash.com/photo-1592396355679-1e2a094e8bf1?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "mavrovo_park",
                        Name = "Mavrovo National Park",
                        City = "Mavrovo",
                        Country = "North Macedonia",
                        Continent = "Europe",
                        Region = "Southeastern Europe",
                        Type = "National Park",
                        Tags = new List<string> { "nature", "skiing", "hiking", "mountains", "wildlife", "lakes" },
                        Description = "Mountain park famous for its unique five-needle pine trees and glacial lakes known as 'Mountain Eyes'.",
                        ImageUrl = "https://images.unsplash.com/photo-1589553416260-f586c8f1514f?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "heraclea_lyncestis",
                        Name = "Heraclea Lyncestis",
                        City = "Bitola",
                        Country = "North Macedonia",
                        Continent = "Europe",
                        Region = "Southeastern Europe",
                        Type = "Archaeological Site",
                        Tags = new List<string> { "archaeology", "history", "ancient", "roman", "mosaics", "culture" },
                        Description = "Ancient Roman city with well-preserved mosaics and amphitheater from the 2nd century BC.",
                        ImageUrl = "https://images.unsplash.com/photo-1588587555470-644d77c1c023?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "stone_dolls",
                        Name = "Kuklica Stone Town",
                        City = "Kratovo",
                        Country = "North Macedonia",
                        Continent = "Europe",
                        Region = "Southeastern Europe",
                        Type = "Natural Monument",
                        Tags = new List<string> { "nature", "geology", "unique", "photography", "hiking", "legends" },
                        Description = "Natural rock formations resembling human figures, created by mineral-rich deposits and erosion.",
                        ImageUrl = "https://images.unsplash.com/photo-1588587555470-644d77c1c023?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "monastery_jovan",
                        Name = "Saint Jovan Bigorski",
                        City = "Debar",
                        Country = "North Macedonia",
                        Continent = "Europe",
                        Region = "Southeastern Europe",
                        Type = "Religious Site",
                        Tags = new List<string> { "monastery", "religious", "architecture", "culture", "history", "art" },
                        Description = "Historic Orthodox monastery known for its intricate wood carvings and religious art.",
                        ImageUrl = "https://images.unsplash.com/photo-1588587555470-644d77c1c023?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "pelister_park",
                        Name = "Pelister National Park",
                        City = "Bitola",
                        Country = "North Macedonia",
                        Continent = "Europe",
                        Region = "Southeastern Europe",
                        Type = "National Park",
                        Tags = new List<string> { "nature", "hiking", "mountains", "wildlife", "molika", "lakes" },
                        Description = "Mountain park famous for its unique five-needle pine trees and glacial lakes known as 'Mountain Eyes'.",
                        ImageUrl = "https://images.unsplash.com/photo-1588587555470-644d77c1c023?q=80&w=2940&auto=format&fit=crop"
                    },
                    new Place
                    {
                        Key = "stobi_ruins",
                        Name = "Stobi Archaeological Site",
                        City = "Gradsko",
                        Country = "North Macedonia",
                        Continent = "Europe",
                        Region = "Southeastern Europe",
                        Type = "Archaeological Site",
                        Tags = new List<string> { "archaeology", "history", "ancient", "roman", "culture", "mosaics" },
                        Description = "Ancient city that was an important urban center from the Hellenistic to the Late Antique period.",
                        ImageUrl = "https://images.unsplash.com/photo-1588587555470-644d77c1c023?q=80&w=2940&auto=format&fit=crop"
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

                // Create relationships between places based on various criteria
                foreach (var place in places)
                {
                    // Connect places in the same city
                    var sameCityPlaces = places.Where(p => p.City == place.City && p.Key != place.Key);
                    foreach (var related in sameCityPlaces)
                    {
                        await CreateRelationshipAsync(place, related, 1.0);
                    }

                    // Connect places in the same country
                    var sameCountryPlaces = places.Where(p => p.Country == place.Country && p.City != place.City);
                    foreach (var related in sameCountryPlaces)
                    {
                        await CreateRelationshipAsync(place, related, 0.8);
                    }

                    // Connect places with similar types
                    var similarTypePlaces = places.Where(p => 
                        p.Type == place.Type && 
                        p.Key != place.Key && 
                        p.Country != place.Country);
                    foreach (var related in similarTypePlaces)
                    {
                        await CreateRelationshipAsync(place, related, 0.6);
                    }

                    // Connect places with similar tags (at least 2 common tags)
                    var similarTagsPlaces = places.Where(p =>
                        p.Key != place.Key &&
                        p.Country != place.Country &&
                        p.Tags.Intersect(place.Tags).Count() >= 2);
                    foreach (var related in similarTagsPlaces)
                    {
                        var commonTags = place.Tags.Intersect(related.Tags).Count();
                        var weight = 0.4 + (commonTags * 0.1); // More common tags = stronger relationship
                        await CreateRelationshipAsync(place, related, weight);
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

        private async Task CreateRelationshipAsync(Place place1, Place place2, double weight)
        {
            try
            {
                var edge = new RelatedPlacesEdge
                {
                    From = $"Places/{place1.Key}",
                    To = $"Places/{place2.Key}",
                    Weight = weight
                };
                await InsertRelatedPlaceEdgeAsync(edge);
                Console.WriteLine($"[ArangoDBHelper] Created relationship: {place1.Name} -> {place2.Name} (weight: {weight})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ArangoDBHelper] Error creating relationship between {place1.Name} and {place2.Name}: {ex.Message}");
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