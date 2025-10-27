using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace api.Functions;

public class GetTransactions
{
    private readonly ILogger<GetTransactions> _logger;

    public GetTransactions(ILogger<GetTransactions> logger)
    {
        _logger = logger;
    }

    [Function("GetTransactions")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}