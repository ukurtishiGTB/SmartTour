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
        /// “People who visited {placeKey} also visited …”
        /// Returns up to 5 RecommendationResult objects, each containing:
        ///   • placeId (full <collection>/<key>),
        ///   • placeName,
        ///   • score (frequency).
        /// </summary>
        public async Task<List<RecommendationResult>> RecommendByPlaceAsync(string placeKey)
        {
            // AQL explanation:
            // 1) Let target = "Places/<placeKey>"
            // 2) Find all users who visited that target via the Visited edge
            // 3) For those users, find other places they visited (excluding target),
            //    then COLLECT and count frequency,
            //    SORT DESC, LIMIT 5.
            var aql = @"
                LET target = CONCAT('Places/', @placeKey)

                // 1) Find all users who visited the target
                LET usersWhoVisited = (
                  FOR v, e IN INBOUND target Visited
                    RETURN v._id
                )

                // 2) For each such user, find other places they visited
                FOR uId IN usersWhoVisited
                  FOR p2, edge2 IN OUTBOUND uId Visited
                    FILTER p2._id != target
                    COLLECT pid = p2._id WITH COUNT INTO freq
                    SORT freq DESC
                    LIMIT 5
                    RETURN {
                      placeId: pid,
                      placeName: p2.name,
                      score: freq
                    }
            ";

            var bindVars = new Dictionary<string, object>
            {
                { "placeKey", placeKey }
            };

            try
            {
                var cursor = await _helper.Client.Cursor.PostCursorAsync<RecommendationResult>(
                    new PostCursorBody
                    {
                        Query    = aql,
                        BindVars = bindVars
                    }
                );
                return cursor.Result.ToList();
            }
            catch (Exception)
            {
                // On any error (e.g., place not found), return an empty list
                return new List<RecommendationResult>();
            }
        }

        /// <summary>
        /// “Places matching {userKey}’s preferences.”
        /// Returns all Place objects whose tags overlap the user’s preferences array.
        /// </summary>
        public async Task<List<Place>> RecommendByTagsAsync(string userKey)
        {
            // AQL explanation:
            // 1) Load the user's document to get its "preferences" array
            // 2) Return all places where any of those preferences appears in p.tags
            var aql = @"
                LET userPrefs = DOCUMENT(CONCAT('Users/', @userKey)).preferences

                FOR p IN Places
                  FILTER LENGTH(INTERSECTION(p.tags, userPrefs)) > 0
                  RETURN p
            ";

            var bindVars = new Dictionary<string, object>
            {
                { "userKey", userKey }
            };

            try
            {
                var cursor = await _helper.Client.Cursor.PostCursorAsync<Place>(
                    new PostCursorBody
                    {
                        Query    = aql,
                        BindVars = bindVars
                    }
                );
                return cursor.Result.ToList();
            }
            catch (Exception)
            {
                return new List<Place>();
            }
        }
    }
}