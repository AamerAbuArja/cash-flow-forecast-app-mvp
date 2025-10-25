using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using api.Models;

namespace api.Functions;

public class GetTransaction
{
     private readonly CosmosClient _cosmosClient;
    private readonly Container _container;

    public GetTransaction(CosmosClient client)
    {
        _cosmosClient = client;
        _container = _cosmosClient.GetContainer("CashflowDB", "Transactions");
    }

    [Function("GetTransactions")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "transactions")] HttpRequestData req)
    {
        var response = req.CreateResponse();
        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string projectId = query["projectId"];
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            if (string.IsNullOrWhiteSpace(projectId))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("{\"error\":\"Missing query parameter: projectId\"}");
                return response;
            }

            var sql = new QueryDefinition("SELECT * FROM c WHERE c.projectId = @projectId")
                .WithParameter("@projectId", projectId);

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
}