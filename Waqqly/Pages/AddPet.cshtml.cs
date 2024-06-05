using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class AddPetModel : PageModel
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;
    private readonly ILogger<AddPetModel> _logger;

    [BindProperty]
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; }

    [BindProperty]
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Type { get; set; }

    [BindProperty]
    [Required]
    [Range(0, 100)]
    public int Age { get; set; }

    [BindProperty]
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Owner { get; set; }

    public AddPetModel(IConfiguration configuration, ILogger<AddPetModel> logger)
    {
        _logger = logger;
        var connectionString = configuration["CosmosDb:ConnectionString"];
        var databaseName = configuration["CosmosDb:DatabaseName"];
        var petCollectionName = configuration["CosmosDb:PetCollectionName"];

        _cosmosClient = new CosmosClient(connectionString);
        var database = _cosmosClient.GetDatabase(databaseName);
        _container = database.GetContainer(petCollectionName);
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

        var newPet = new Pet
        {
            Id = Guid.NewGuid().ToString(),
            Name = Name,
            Type = Type,
            Age = Age,
            Owner = Owner
        };

        try
        {
            _logger.LogInformation("Attempting to write to Cosmos DB.");
            ItemResponse<Pet> response = await _container.CreateItemAsync(newPet, new PartitionKey(newPet.Id));
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

public class Pet
{
    [JsonProperty("id")]
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public int Age { get; set; }
    public string Owner { get; set; }
}

