using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
string connectionString = Environment.GetEnvironmentVariable("CosmosDBConnectionString");
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
CosmosClient cosmosClient = new CosmosClient(connectionString);
builder.Services.AddSingleton(cosmosClient);

// builder.Services.AddSingleton<TaxCalculator>();
builder.Services.Configure<KestrelServerOptions>(options =>
    {
        options.AllowSynchronousIO = true;
    });
builder.Services.Configure<IISServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});
builder.Build().Run();
