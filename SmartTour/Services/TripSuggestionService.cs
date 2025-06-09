using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using SmartTour.Data;
using SmartTour.Models;

namespace SmartTour.Services
{
    public class TripSuggestionService
    {
        private readonly ArangoDBHelper _helper;
        private const string CollectionName = "TripSuggestions";

        public TripSuggestionService(ArangoDBHelper helper)
        {
            _helper = helper;
        }

        public async Task<TripSuggestion> GetByKeyAsync(string key)
        {
            try
            {
                return await _helper.Client.Document.GetDocumentAsync<TripSuggestion>(
                    CollectionName,
                    key
                );
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<TripSuggestion>> GetByUserKeyAsync(string userKey)
        {
            const string aql = @"
                FOR s IN TripSuggestions
                    FILTER s.userKey == @userKey
                    SORT s.createdAt DESC
                    RETURN s
            ";

            var bindVars = new Dictionary<string, object>
            {
                ["userKey"] = userKey
            };

            var cursor = await _helper.Client.Cursor.PostCursorAsync<TripSuggestion>(
                new PostCursorBody
                {
                    Query = aql,
                    BindVars = bindVars
                }
            );

            return cursor.Result.ToList();
        }

        public async Task<string> CreateAsync(TripSuggestion suggestion)
        {
            var response = await _helper.Client.Document.PostDocumentAsync(
                CollectionName,
                suggestion
            );
            return response._id;
        }

        public async Task<bool> UpdateAsync(string key, TripSuggestion suggestion)
        {
            try
            {
                await _helper.Client.Document.PutDocumentAsync(
                    CollectionName,
                    key,
                    suggestion
                );
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
} 