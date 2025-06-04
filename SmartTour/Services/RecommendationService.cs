// File: SmartTour/Data/Services/RecommendationService.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ArangoDB driver namespaces
using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;

// Your helper + model namespaces
using SmartTour.Data;         // for ArangoDBHelper
using SmartTour.Models;  // for Place, RecommendationResult

namespace SmartTour.Services
{
    public class RecommendationService
    {
        private readonly ArangoDBHelper _helper;

        public RecommendationService(ArangoDBHelper helper)
        {
            _helper = helper;
        }

        /// <summary>
        /// "People who visited {placeKey} also visited …"
        /// Returns up to 5 RecommendationResult objects, each containing:
        ///   • placeId (full <collection>/<key>),
        ///   • placeName,
        ///   • score (frequency).
        /// </summary>
        public async Task<List<RecommendationResult>> RecommendByPlaceAsync(string placeKey)
        {
            // AQL query to find places visited by users who also visited the target place
            var aql = @"
                LET target = CONCAT('Places/', @placeKey)
                FOR u IN OUTBOUND target Visited
                    FOR p IN OUTBOUND u Visited
                        FILTER p._id != target
                        COLLECT placeId = p._id, placeName = p.name INTO occurrences
                        SORT COUNT(occurrences) DESC
                        LIMIT 5
                        RETURN {
                            placeId: placeId,
                            placeName: placeName,
                            score: COUNT(occurrences)
                        }
            ";

            var cursor = await _helper.Client.Cursor.PostCursorAsync<RecommendationResult>(
                new PostCursorBody
                {
                    Query = aql,
                    BindVars = new Dictionary<string, object> { { "placeKey", placeKey } }
                }
            );

            return cursor.Result.ToList();
        }

        /// <summary>
        /// Recommends places based on user's preferences/tags
        /// </summary>
        public async Task<List<Place>> RecommendByTagsAsync(string userKey)
        {
            // AQL query to find places with tags matching user preferences
            var aql = @"
                LET user = DOCUMENT(CONCAT('Users/', @userKey))
                FOR p IN Places
                    LET matchingTags = LENGTH(
                        FOR tag IN p.tags
                            FILTER tag IN user.preferences
                            RETURN 1
                    )
                    FILTER matchingTags > 0
                    SORT matchingTags DESC
                    LIMIT 10
                    RETURN p
            ";

            var cursor = await _helper.Client.Cursor.PostCursorAsync<Place>(
                new PostCursorBody
                {
                    Query = aql,
                    BindVars = new Dictionary<string, object> { { "userKey", userKey } }
                }
            );

            return cursor.Result.ToList();
        }

        /// <summary>
        /// Find places connected through a path of related places
        /// Uses graph traversal to find paths between places
        /// </summary>
        public async Task<List<Place>> FindConnectedPlacesAsync(string startPlaceKey, int maxDepth = 3)
        {
            var aql = @"
                LET start = CONCAT('Places/', @startPlaceKey)
                FOR v, e, p IN 1..@maxDepth OUTBOUND start RelatedPlaces
                    RETURN DISTINCT v
            ";

            var cursor = await _helper.Client.Cursor.PostCursorAsync<Place>(
                new PostCursorBody
                {
                    Query = aql,
                    BindVars = new Dictionary<string, object> { 
                        { "startPlaceKey", startPlaceKey },
                        { "maxDepth", maxDepth }
                    }
                }
            );

            return cursor.Result.ToList();
        }

        /// <summary>
        /// Find users with similar travel patterns
        /// </summary>
        public async Task<List<User>> FindSimilarUsersAsync(string userKey, int minCommonPlaces = 2)
        {
            var aql = @"
                LET userPlaces = (
                    FOR p IN OUTBOUND CONCAT('Users/', @userKey) Visited
                        RETURN p._id
                )
                FOR u IN Users
                    FILTER u._key != @userKey
                    LET commonPlaces = (
                        FOR p IN OUTBOUND u Visited
                            FILTER p._id IN userPlaces
                            RETURN p._id
                    )
                    FILTER LENGTH(commonPlaces) >= @minCommonPlaces
                    SORT LENGTH(commonPlaces) DESC
                    RETURN u
            ";

            var cursor = await _helper.Client.Cursor.PostCursorAsync<User>(
                new PostCursorBody
                {
                    Query = aql,
                    BindVars = new Dictionary<string, object> { 
                        { "userKey", userKey },
                        { "minCommonPlaces", minCommonPlaces }
                    }
                }
            );

            return cursor.Result.ToList();
        }

        /// <summary>
        /// Generates a personalized trip recommendation for a user
        /// based on their preferences and past visits
        /// </summary>
        /// <param name="userKey">The user's key</param>
        /// <param name="tripDuration">Desired trip duration in days</param>
        /// <param name="maxPlaces">Maximum number of places to recommend</param>
        /// <returns>A list of recommended places for a trip</returns>
        public async Task<List<Place>> GeneratePersonalizedTripAsync(string userKey, int tripDuration = 7, int maxPlaces = 5)
        {
            // This complex AQL query combines multiple recommendation strategies:
            // 1. Places matching user preferences
            // 2. Popular places in cities that match user interests
            // 3. Related places based on connections
            var aql = @"
                LET user = DOCUMENT(CONCAT('Users/', @userKey))
                LET visitedPlaces = (
                    FOR p IN OUTBOUND user Visited
                        RETURN p._id
                )
                
                // Strategy 1: Places matching user preferences
                LET preferenceMatches = (
                    FOR p IN Places
                        FILTER p._id NOT IN visitedPlaces
                        LET matchingTags = LENGTH(
                            FOR tag IN p.tags
                                FILTER tag IN user.preferences
                                RETURN 1
                        )
                        FILTER matchingTags > 0
                        SORT matchingTags DESC
                        LIMIT 10
                        RETURN MERGE(p, { score: matchingTags })
                )
                
                // Strategy 2: Popular places in cities with matching interests
                LET cityMatches = (
                    FOR p IN Places
                        FILTER p._id NOT IN visitedPlaces
                        LET cityPlaces = (
                            FOR cp IN Places
                                FILTER cp.city == p.city
                                FOR tag IN cp.tags
                                    FILTER tag IN user.preferences
                                    RETURN 1
                        )
                        LET cityScore = LENGTH(cityPlaces)
                        FILTER cityScore > 0
                        SORT cityScore DESC
                        LIMIT 10
                        RETURN MERGE(p, { score: cityScore })
                )
                
                // Strategy 3: Related places through connections
                LET relatedPlaces = (
                    FOR p IN Places
                        FILTER p._id NOT IN visitedPlaces
                        FOR related IN 1..2 ANY p RelatedPlaces
                            FOR tag IN related.tags
                                FILTER tag IN user.preferences
                                COLLECT place = p, score = COUNT(tag)
                                SORT score DESC
                                LIMIT 10
                                RETURN MERGE(place, { score: score })
                )
                
                // Combine all strategies and rank
                LET allRecommendations = UNION(
                    FOR p IN preferenceMatches RETURN p,
                    FOR p IN cityMatches RETURN p,
                    FOR p IN relatedPlaces RETURN p
                )
                
                // Final ranking and deduplication
                FOR p IN allRecommendations
                    COLLECT place = p INTO scores = p.score
                    LET finalScore = SUM(scores[*])
                    SORT finalScore DESC
                    LIMIT @maxPlaces
                    RETURN MERGE(place, { score: finalScore })
            ";

            var cursor = await _helper.Client.Cursor.PostCursorAsync<Place>(
                new PostCursorBody
                {
                    Query = aql,
                    BindVars = new Dictionary<string, object> { 
                        { "userKey", userKey },
                        { "maxPlaces", maxPlaces }
                    }
                }
            );

            var results = cursor.Result.ToList();

            // If no results found using preferences, return popular places
            if (!results.Any())
            {
                var fallbackAql = @"
                    FOR p IN Places
                        SORT RAND()
                        LIMIT @maxPlaces
                        RETURN p
                ";

                var fallbackCursor = await _helper.Client.Cursor.PostCursorAsync<Place>(
                    new PostCursorBody
                    {
                        Query = fallbackAql,
                        BindVars = new Dictionary<string, object> { { "maxPlaces", maxPlaces } }
                    }
                );

                results = fallbackCursor.Result.ToList();
            }

            return results;
        }
    
        /// <summary>
        /// Recommends places based on current location/city
        /// </summary>
        /// <param name="cityName">The city name to find nearby attractions</param>
        /// <returns>List of places in or near the specified city</returns>
        public async Task<List<Place>> RecommendNearbyPlacesAsync(string cityName)
        {
            var aql = @"
                FOR p IN Places
                    FILTER p.city == @cityName OR p.country == @cityName
                    SORT RAND()
                    LIMIT 10
                    RETURN p
            ";
        
            var cursor = await _helper.Client.Cursor.PostCursorAsync<Place>(
                new PostCursorBody
                {
                    Query = aql,
                    BindVars = new Dictionary<string, object> { { "cityName", cityName } }
                }
            );
        
            return cursor.Result.ToList();
        }
    }
}