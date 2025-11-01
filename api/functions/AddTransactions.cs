using System.Diagnostics;
using System.Net; // here
using System.Text.Json;
using api.Models; // here 2
using FluentValidation;
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
	private readonly IValidator<Transaction> _validator;

	public AddTransactions(ILogger<AddTransactions> logger, CosmosClient client, IValidator<Transaction> validator)
	{
		_logger = logger;
		_cosmosClient = client;
		_container = _cosmosClient.GetContainer("CashflowDB", "Transaction");
		_validator = validator;
	}

	[Function("AddTransactions")]
	public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "transaction")] HttpRequestData req, FunctionContext context)
	{
		var requestId = context.InvocationId?.ToString();
		_logger.LogInformation("HTTP ingest called. InvocationId={InvocationId}", requestId);

		{
			// Start timing the operation for performance monitoring
			var stopwatch = Stopwatch.StartNew();

			_logger.LogInformation("Received a batch transaction POST request at {time}.", DateTime.UtcNow);

			// Read the incoming HTTP request body as a raw string
			string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

			// Declare variable to hold deserialized transactions
			List<Transaction>? transactions;

			try
			{
				// Deserialize the JSON request body into a list of Transaction objects
				transactions = JsonSerializer.Deserialize<List<Transaction>>(requestBody, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true // Allow flexibility in JSON property name casing
				});
			}
			catch (JsonException ex)
			{
				// If JSON parsing fails, return a Bad Request response
				_logger.LogError("Invalid JSON payload: {message}", ex.Message);
				var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
				await badResponse.WriteStringAsync("Invalid JSON format. Expected an array of transactions.");
				return badResponse;
			}

			// Validate that there are transactions in the payload
			if (transactions == null || !transactions.Any())
			{
				var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
				await badResponse.WriteStringAsync("No transactions provided.");
				return badResponse;
			}

			// Initialize collections for valid and invalid transactions
			var validTransactions = new List<Transaction>();
			var invalidTransactions = new List<(Transaction? Txn, string Reason)>();

			foreach (var txn in transactions)
			{
				// Validate each transaction using FluentValidation
				var validationResult = _validator.Validate(txn);
				if (validationResult.IsValid)
				{
					validTransactions.Add(txn);
				}
				else
				{
					var reasons = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
					_logger.LogWarning("Validation failed for transaction (senderTransactionId={senderId}, tenant={tenant}) InvocationId={InvocationId}. Errors: {errors}", txn.SenderTransactionId ?? "(null)", txn.TenantId ?? "(null)", requestId, reasons);
					invalidTransactions.Add((txn, reasons));
				}
			}


			// If no valid transactions exist, return a warning and abort further processing
			if (!validTransactions.Any())
			{
				_logger.LogWarning("All provided transactions are invalid. Count: {count}", invalidTransactions.Count);
				var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
				await badResponse.WriteStringAsync("All transactions are invalid. Nothing to upsert.");
				return badResponse;
			}

			int successCount = 0, failureCount = 0;

			// Parallel upsert for better performance
			var batchTasks = validTransactions.Select(async txn =>
			{
				try
				{
					// Upsert (insert or update) transaction document by TenantId partition key
					await _container.UpsertItemAsync(txn, new PartitionKey(txn.TenantId));
					Interlocked.Increment(ref successCount); // Thread-safe counter increment
				}
				catch (CosmosException ex)
				{
					// Log Cosmos DB-related errors (e.g., 429 throttling, 400 bad request)
					Interlocked.Increment(ref failureCount);
					_logger.LogError("Failed to upsert transaction {id} (Tenant: {tenant}) | Cosmos error {code}: {message}", txn.Id, txn.TenantId, ex.StatusCode, ex.Message);
				}
				catch (Exception ex)
				{
					// Catch any unexpected general exception
					Interlocked.Increment(ref failureCount);
					_logger.LogError("Failed to upsert transaction {id} (Tenant: {tenant}) | Unexpected error: {error}", txn.Id, txn.TenantId, ex.Message);
				}
			});

			// Wait for all upsert tasks to complete
			await Task.WhenAll(batchTasks);

			stopwatch.Stop();
			_logger.LogInformation("Batch upsert completed: {successCount} succeeded, {failureCount} failed, {invalidCount} invalid. Duration: {duration} ms",
					successCount, failureCount, invalidTransactions.Count, stopwatch.ElapsedMilliseconds);

			// -----------------------
			// Prepare and return response summary
			// -----------------------
			var response = req.CreateResponse(HttpStatusCode.OK);

			// Build a summary object including counts, duration, and invalid transaction reasons
			var summary = new
			{
				SuccessCount = successCount,
				FailureCount = failureCount,
				InvalidCount = invalidTransactions.Count,
				DurationMs = stopwatch.ElapsedMilliseconds,
				InvalidDetails = invalidTransactions.Select(v => new
				{
					TransactionId = v.Txn?.Id,
					Reason = v.Reason
				})
			};

			// Return response as JSON
			await response.WriteAsJsonAsync(summary);
			return response;


			// 	try
			// 	{

			// 		// ✅ Use Upsert instead of CreateItem
			// 		var result = await _container.UpsertItemAsync(transaction, new PartitionKey(transaction.TenantId));

			// 		var response = req.CreateResponse(result.StatusCode);
			// 		await response.WriteStringAsync($"Transaction {transaction.Id} upserted successfully.");
			// 		return response;
			// 	}
			// 	catch (CosmosException ex)
			// 	{
			// 		_logger.LogError($"Cosmos DB error: {ex.StatusCode} - {ex.Message}");
			// 		var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
			// 		await response.WriteStringAsync($"Database error: {ex.Message}");
			// 		return response;
			// 	}
			// 	catch (System.Exception ex)
			// 	{
			// 		_logger.LogError($"Unexpected error: {ex.Message}");
			// 		var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
			// 		await response.WriteStringAsync($"Unexpected error: {ex.Message}");
			// 		return response;
			// 	}
		}

		// [Function("GetTransactions")]
		// public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
		// {
		//     _logger.LogInformation("C# HTTP trigger function processed a request.");
		//     return new OkObjectResult("Welcome to Azure Functions!");
		// }
	}
}