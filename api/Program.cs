using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Server.Kestrel.Core; // here
using Microsoft.Azure.Cosmos; // here
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FluentValidation; // here
using api.Validators; // here
using FluentValidation.AspNetCore;
// using Microsoft.AspNetCore.Builder; // here

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

builder.Services.AddValidatorsFromAssemblyContaining<TransactionValidator>();

builder.Services.Configure<KestrelServerOptions>(options =>
{
	options.AllowSynchronousIO = true;
});

// المشكلة كانت في هذا الكود الخرى تحت
// builder.Services.Configure<IISServerOptions>(options =>
// {
//     options.AllowSynchronousIO = true;
// });

builder.Build().Run();
