using System.Net;
using System.Text.Json;
using api.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Functions
{
    public class AddTransactions2
    {
        // Dependency-injected logger instance used for structured logging throughout the function
        private readonly ILogger<AddTransactions2> _logger;

        // Cosmos DB client used to interact with the database
        private readonly CosmosClient _cosmosClient;

        // Reference to a specific Cosmos DB container ("Transaction" in "CashflowDB")
        private readonly Container _container;

        // Constructor - runs once when the Azure Function is initialized
        // Dependencies (logger and CosmosClient) are injected automatically by Azure Functions' DI
        public AddTransactions2(ILogger<AddTransactions2> logger, CosmosClient client)
        {
            _logger = logger;
            _cosmosClient = client;

            // Get a handle to the Cosmos DB container for transactions
            _container = _cosmosClient.GetContainer("CashflowDB", "Transaction");
        }

        // Azure Function definition:
        //  - HTTP triggered ("post" requests)
        //  - Authorization level: Function (requires function key)
        //  - Route: POST /api/transactions/batch
        [Function("AddTransactions2")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "transactions/batch")] HttpRequestData req,
            FunctionContext context)
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

            // Validate each transaction using a custom validator
            foreach (var txn in transactions)
            {
                var validationResult = ValidateTransaction(txn);

                if (validationResult == "OK")
                {
                    // Valid transaction -> add to the valid list
                    validTransactions.Add(txn);
                }
                else
                {
                    // Invalid transaction -> record the reason
                    invalidTransactions.Add((txn, validationResult));
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

            // -----------------------
            // Perform bulk upsert into Cosmos DB
            // -----------------------
            int successCount = 0, failureCount = 0;
            var container = _container;

            // Parallel upsert for better performance
            var batchTasks = validTransactions.Select(async txn =>
            {
                try
                {
                    // Upsert (insert or update) transaction document by TenantId partition key
                    await container.UpsertItemAsync(txn, new PartitionKey(txn.TenantId));
                    Interlocked.Increment(ref successCount); // Thread-safe counter increment
                }
                catch (CosmosException ex)
                {
                    // Log Cosmos DB-related errors (e.g., 429 throttling, 400 bad request)
                    Interlocked.Increment(ref failureCount);
                    _logger.LogError("Failed to upsert transaction {id} (Tenant: {tenant}) | Cosmos error {code}: {message}",
                        txn.Id, txn.TenantId, ex.StatusCode, ex.Message);
                }
                catch (Exception ex)
                {
                    // Catch any unexpected general exception
                    Interlocked.Increment(ref failureCount);
                    _logger.LogError("Failed to upsert transaction {id} (Tenant: {tenant}) | Unexpected error: {error}",
                        txn.Id, txn.TenantId, ex.Message);
                }
            });

            // Wait for all upsert tasks to complete
            await Task.WhenAll(batchTasks);

            // Stop timer and log summary
            stopwatch.Stop();
            _logger.LogInformation("Batch upsert completed: {successCount} succeeded, {failureCount} failed, {invalidCount} invalid. Duration: {duration} ms",
                successCount, failureCount, invalidTransactions.Count, stopwatch.ElapsedMilliseconds);

            // -----------------------
            // Fire-and-forget aggregation trigger
            // -----------------------
            // This asynchronously calls another function responsible for recalculating aggregates
            // It doesn’t block the main HTTP request
            _ = Task.Run(async () =>
            {
                try
                {
                    // Get the aggregation function URL from environment variables
                    var aggregationFunctionUrl = Environment.GetEnvironmentVariable("AggregationFunctionUrl");

                    if (!string.IsNullOrEmpty(aggregationFunctionUrl))
                    {
                        using var httpClient = new HttpClient();

                        // Extract unique tenant IDs from successfully processed transactions
                        var tenantIds = validTransactions.Select(t => t.TenantId).Distinct().ToList();

                        // Prepare JSON payload with list of tenants to aggregate
                        var payload = JsonSerializer.Serialize(new { tenants = tenantIds });
                        var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

                        // Call the aggregation function
                        var response = await httpClient.PostAsync(aggregationFunctionUrl, content);

                        _logger.LogInformation("Triggered aggregation function for {count} tenants. Status: {statusCode}",
                            tenantIds.Count, response.StatusCode);
                    }
                    else
                    {
                        _logger.LogWarning("AggregationFunctionUrl environment variable not set — skipping aggregation trigger.");
                    }
                }
                catch (Exception ex)
                {
                    // Log any failures during asynchronous trigger
                    _logger.LogError("Failed to trigger aggregation function asynchronously: {message}", ex.Message);
                }
            });

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
        }

        // -----------------------------------
        // Custom validation method (placeholder)
        // -----------------------------------
        // Should return "OK" if valid, otherwise a string describing why it's invalid.
        private string ValidateTransaction(Transaction txn)
        {
            // Example validation (you can replace with your own logic)
            if (txn == null)
                return "Transaction is null";

            if (string.IsNullOrWhiteSpace(txn.Id.ToString()))
                return "Missing transaction ID";

            if (string.IsNullOrWhiteSpace(txn.TenantId))
                return "Missing Tenant ID";

            if (txn.Amount <= 0)
                return "Invalid transaction amount";

            return "OK";
        }
    }
}
