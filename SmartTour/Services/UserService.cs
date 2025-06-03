// File: SmartTour/Data/Services/UserService.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using SmartTour.Data;            // for ArangoDBHelper
using SmartTour.Models;     // for User

namespace SmartTour.Services
{
    public class UserService
    {
        private readonly ArangoDBHelper _helper;

        public UserService(ArangoDBHelper helper)
        {
            _helper = helper;
        }

        /// <summary>
        /// 1. List all users (returns a List<User>, not IEnumerable).
        ///    cursor.Result is already List<User>, so we just return it.
        /// </summary>
        public async Task<List<User>> GetAllAsync()
        {
            // AQL that returns all documents in the “Users” collection:
            var aql = "FOR u IN Users RETURN u";

            // PostCursorAsync<User> returns a CursorResponse whose .Result is List<User>
            var cursor = await _helper.Client.Cursor.PostCursorAsync<User>(
                new PostCursorBody { Query = aql }
            );

            return cursor.Result.ToList(); 
        }

        /// <summary>
        /// 2. Get one user by key. If not found, return null.
        /// </summary>
        public async Task<User> GetByKeyAsync(string key)
        {
            try
            {
                // Note: this now returns User directly, not a wrapper with .Document
                var user = await _helper.Client.Document.GetDocumentAsync<User>(
                    "Users", // collection name
                    key      // document _key
                );
                return user;
            }
            catch (Exception)
            {
                // Any exception (including “not found”) is swallowed, returning null
                return null;
            }
        }

        /// <summary>
        /// 3. Create a new user. Returns the new document’s _id (“Users/&lt;key&gt;”).
        /// </summary>
        public async Task<string> CreateAsync(User user)
        {
            // PostDocumentAsync<T>(collectionName, documentObject)
            var response = await _helper.Client.Document.PostDocumentAsync<User>(
                "Users",
                user
            );
            return response._id; 
        }

        /// <summary>
        /// 4. Update an existing user. 
        ///    We pass an anonymous object with only the fields we want to change.
        /// </summary>
        public async Task<bool> UpdateAsync(string key, User updated)
        {
            // Build a partial/patch object with just the properties you want to update:
            try
            {
                // PutDocumentAsync<T>(collectionName, documentKey, updatedDocument)
                await _helper.Client.Document.PutDocumentAsync<User>(
                    "Users",   // collection name
                    key,       // document _key
                    updated    // the entire new User object
                );
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
            
        }

        /// <summary>
        /// 5. Delete a user by key.
        /// </summary>
        public async Task<bool> DeleteAsync(string key)
        {
            await _helper.Client.Document.DeleteDocumentAsync(
                "Users",  // collection name
                key       // document _key
            );
            return true;
        }
    }
}