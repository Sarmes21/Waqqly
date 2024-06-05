using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

public class AddWalkerModel : PageModel
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;
    private readonly ILogger<AddWalkerModel> _logger;

    [BindProperty]
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; }

    [BindProperty]
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [BindProperty]
    [Required]
    [Phone]
    public string Phone { get; set; }

    [BindProperty]
    [Required]
    public string Location { get; set; }

    public AddWalkerModel(IConfiguration configuration, ILogger<AddWalkerModel> logger)
    {
        _logger = logger;
        var connectionString = configuration["CosmosDb:ConnectionString"];
        var databaseName = configuration["CosmosDb:DatabaseName"];
        var collectionName = configuration["CosmosDb:CollectionName"];

        _cosmosClient = new CosmosClient(connectionString);
        var database = _cosmosClient.GetDatabase(databaseName);
        _container = database.GetContainer(collectionName);
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var id = Guid.NewGuid().ToString(); // Generate one GUID for both id and Id

        var newWalker = new DogWalker
        {
            id = id,
            Id = id, // Use the same GUID for partition key
            Name = Name,
            Email = Email,
            Phone = Phone,
            Location = Location
        };

        try
        {
            _logger.LogInformation("Attempting to write to Cosmos DB.");
            ItemResponse<DogWalker> response = await _container.CreateItemAsync(newWalker, new PartitionKey(newWalker.Id));
            _logger.LogInformation($"Successfully written to Cosmos DB with status code: {response.StatusCode}");
            return RedirectToPage("Index");
        }
        catch (CosmosException ex)
        {
            _logger.LogError($"CosmosException occurred: {ex.StatusCode} - {ex.Message}");
            ModelState.AddModelError(string.Empty, $"Cosmos DB Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception occurred: {ex.Message}");
            ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
        }

        return Page();
    }
}

public class DogWalker
{
    [JsonProperty(PropertyName = "id")]
    public string id { get; set; }
    public string Id { get; set; } // Partition key
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Location { get; set; }
}
