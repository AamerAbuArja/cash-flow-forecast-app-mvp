using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Transaction.Actions;

public class GetTransaction
{
    private readonly ILogger<GetTransaction> _logger;

    public GetTransaction(ILogger<GetTransaction> logger)
    {
        _logger = logger;
    }

    [Function("GetTransaction")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}