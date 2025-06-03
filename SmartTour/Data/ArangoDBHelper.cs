// SmartTour/Data/ArangoDBHelper.cs
using System;
using System.Threading.Tasks;
using ArangoDBNetStandard;
using ArangoDBNetStandard.Transport.Http;
using Microsoft.Extensions.Configuration;

namespace SmartTour.Data
{
    public class ArangoDBHelper
    {
        private readonly ArangoDBClient _arango;

        public ArangoDBHelper(IConfiguration configuration)
        {
            // 1. Read the "Arango" section from appsettings.json:
            var arangoSection = configuration.GetSection("Arango");
            var url      = arangoSection.GetValue<string>("Url");
            var database = arangoSection.GetValue<string>("Database");
            var username = arangoSection.GetValue<string>("Username");
            var password = arangoSection.GetValue<string>("Password");

            // 2. Build the HttpApiTransport using basic auth:
            var transport = HttpApiTransport.UsingBasicAuth(
                new Uri(url),   // e.g. "http://127.0.0.1:8529/"
                database,       // e.g. "MyDatabaseName"
                username,       // e.g. "root"
                password        // e.g. "yourRootPassword"
            );

            // 3. Instantiate the client
            _arango = new ArangoDBClient(transport);
        }

        /// <summary>
        /// Expose the ArangoDBClient so controllers/services can call it.
        /// </summary>
        public ArangoDBClient Client => _arango;

        // Example helper methods:
        public async Task<bool> IsServerAliveAsync()
        {
            try
            {
                var versionInfo = await _arango.Admin.GetServerVersionAsync();
                Console.WriteLine($"ArangoDB version: {versionInfo.Version}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ... (other CRUD/AQL methods, as shown previously) ...
    }
}