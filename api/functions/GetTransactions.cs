using System.Net; // here
using api.Models; // here 2
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos; // here 2
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http; // here
using Microsoft.Extensions.Logging;
using Newtonsoft.Json; // here 2

namespace api.Functions;

public class GetTransactions
{
    private readonly ILogger<GetTransactions> _logger;
	private readonly CosmosClient _cosmosClient;
	private readonly Container _container;
    public GetTransactions(ILogger<GetTransactions> logger, CosmosClient client)
    {
        _logger = logger;
		_cosmosClient = client;
		_container = _cosmosClient.GetContainer("CashflowDB", "Transaction");
    }

    [Function("GetTransactions")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "transaction")] HttpRequestData req)
    {
        var response = req.CreateResponse();
		try
		{
			var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
			string tenantId = query["tenantId"];
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

			if (string.IsNullOrWhiteSpace(tenantId))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("{\"error\":\"Missing query parameter: tenantId\"}");
                return response;
            }

			var sql = new QueryDefinition("SELECT * FROM c WHERE c.tenantId = @tenantId")
					.WithParameter("@tenantId", tenantId);

			var results = new List<Transaction>();
			using var iterator = _container.GetItemQueryIterator<Transaction>(sql);
			while (iterator.HasMoreResults)
			{
				var page = await iterator.ReadNextAsync();
				results.AddRange(page);
			}

			response.StatusCode = HttpStatusCode.OK;
			response.Headers.Add("Content-Type", "application/json");
			await response.WriteStringAsync(JsonConvert.SerializeObject(results, Formatting.Indented));
		}
		catch (CosmosException ex)
		{
			response.StatusCode = (HttpStatusCode)ex.StatusCode;
			await response.WriteStringAsync(JsonConvert.SerializeObject(new
			{
				error = ex.Message,
				statusCode = ex.StatusCode
			}));
		}
		catch (Exception ex)
		{
			response.StatusCode = HttpStatusCode.InternalServerError;
			await response.WriteStringAsync(JsonConvert.SerializeObject(new
			{
				error = ex.Message
			}));
		}

		return response;
    }


    // [Function("GetTransactions")]
    // public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    // {
    //     _logger.LogInformation("C# HTTP trigger function processed a request.");
    //     return new OkObjectResult("Welcome to Azure Functions!");
    // }
}