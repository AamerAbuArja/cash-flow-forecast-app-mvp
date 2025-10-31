using System.Net; // here
using System.Text.Json;
using api.Models; // here 2
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos; // here 2
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http; // here
using Microsoft.Extensions.Logging;
// using Newtonsoft.Json; // here 2

namespace api.Functions;

public class AddTransactions
{
	private readonly ILogger<AddTransactions> _logger;
	private readonly CosmosClient _cosmosClient;
	private readonly Container _container;
	public AddTransactions(ILogger<AddTransactions> logger, CosmosClient client)
	{
		_logger = logger;
		_cosmosClient = client;
		_container = _cosmosClient.GetContainer("CashflowDB", "Transaction");
	}

	[Function("AddTransactions")]
	public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "transaction")] HttpRequestData req)
	{
		_logger.LogInformation("Received a transaction POST request.");

		string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
		Transaction transaction;

		try
		{
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            transaction = JsonSerializer.Deserialize<Transaction>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        }
		catch (JsonException)
		{
			var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
			await badResponse.WriteStringAsync("Invalid JSON payload.");
			return badResponse;
		}

		if (transaction == null || string.IsNullOrWhiteSpace(transaction.Id.ToString()) || string.IsNullOrWhiteSpace(transaction.TenantId))
		{
			var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
			await badResponse.WriteStringAsync("Missing required fields: id or tenantId.");
			return badResponse;
		}

		try
		{

			// ✅ Use Upsert instead of CreateItem
			var result = await _container.UpsertItemAsync(transaction, new PartitionKey(transaction.TenantId));

			var response = req.CreateResponse(result.StatusCode);
			await response.WriteStringAsync($"Transaction {transaction.Id} upserted successfully.");
			return response;
		}
		catch (CosmosException ex)
		{
			_logger.LogError($"Cosmos DB error: {ex.StatusCode} - {ex.Message}");
			var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
			await response.WriteStringAsync($"Database error: {ex.Message}");
			return response;
		}
		catch (System.Exception ex)
		{
			_logger.LogError($"Unexpected error: {ex.Message}");
			var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
			await response.WriteStringAsync($"Unexpected error: {ex.Message}");
			return response;
		}
	}


    // [Function("GetTransactions")]
    // public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    // {
    //     _logger.LogInformation("C# HTTP trigger function processed a request.");
    //     return new OkObjectResult("Welcome to Azure Functions!");
    // }
}