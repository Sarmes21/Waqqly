using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Waqqly.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly Container _walkerContainer;
        private readonly Container _petContainer;

        public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration)
        {
            _logger = logger;

            var connectionString = configuration["CosmosDb:ConnectionString"];
            var databaseName = configuration["CosmosDb:DatabaseName"];
            var walkerCollectionName = "Walkers";
            var petCollectionName = "Pets";

            _cosmosClient = new CosmosClient(connectionString);
            var database = _cosmosClient.GetDatabase(databaseName);
            _walkerContainer = database.GetContainer(walkerCollectionName);
            _petContainer = database.GetContainer(petCollectionName);

            Walkers = new List<DogWalker>();
            Pets = new List<Pet>();

            FetchData().GetAwaiter().GetResult();
        }

        public List<DogWalker> Walkers { get; private set; }
        public List<Pet> Pets { get; private set; }

        private async Task FetchData()
        {
            var walkerQuery = "SELECT * FROM c";
            var petQuery = "SELECT * FROM c";

            var walkerIterator = _walkerContainer.GetItemQueryIterator<DogWalker>(new QueryDefinition(walkerQuery));
            var petIterator = _petContainer.GetItemQueryIterator<Pet>(new QueryDefinition(petQuery));

            var walkers = new List<DogWalker>();
            var pets = new List<Pet>();

            while (walkerIterator.HasMoreResults)
            {
                var walkerResponse = await walkerIterator.ReadNextAsync();
                walkers.AddRange(walkerResponse.ToList());
            }

            while (petIterator.HasMoreResults)
            {
                var petResponse = await petIterator.ReadNextAsync();
                pets.AddRange(petResponse.ToList());
            }

            Walkers = walkers;
            Pets = pets;
        }

        public async Task<IActionResult> OnPostRemoveWalkerAsync(string walkerId)
        {
            if (!string.IsNullOrEmpty(walkerId))
            {
                try
                {
                    var partitionKey = new PartitionKey(walkerId);
                    var response = await _walkerContainer.DeleteItemAsync<DogWalker>(walkerId, partitionKey);

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogError($"Walker with ID {walkerId} not found.");
                    }
                    else
                    {
                        _logger.LogInformation($"Walker with ID {walkerId} removed successfully.");
                    }

                    await FetchData();
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError($"Walker with ID {walkerId} not found.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"An error occurred while removing walker with ID {walkerId}: {ex.Message}");
                }
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemovePetAsync(string petId)
        {
            if (!string.IsNullOrEmpty(petId))
            {
                try
                {
                    var partitionKey = new PartitionKey(petId);
                    var response = await _petContainer.DeleteItemAsync<Pet>(petId, partitionKey);

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogError($"Pet with ID {petId} not found.");
                    }
                    else
                    {
                        _logger.LogInformation($"Pet with ID {petId} removed successfully.");
                    }

                    await FetchData();
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError($"Pet with ID {petId} not found.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"An error occurred while removing pet with ID {petId}: {ex.Message}");
                }
            }

            return RedirectToPage();
        }
    }

    public class DogWalker
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Location { get; set; }
    }

    public class Pet
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int Age { get; set; }
        public string Owner { get; set; }
    }
}
