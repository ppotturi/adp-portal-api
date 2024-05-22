using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics.CodeAnalysis;
using ADP.Portal.Api.Config;

namespace ADP.Portal.Api.Extensions;

[ExcludeFromCodeCoverage]
public static class BuilderExtensions
{
    public static WebApplicationBuilder ConfigureOpenTelemetry(this WebApplicationBuilder builder, AppInsightsConfig appInsightsConfig)
    {
        if (!string.IsNullOrEmpty(appInsightsConfig.ConnectionString))
        {
            builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
            {
                options.ConnectionString = appInsightsConfig.ConnectionString;
                options.Credential = new DefaultAzureCredential();
                string env = builder.Configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") ?? "Development";
                if (!env.Equals("Development"))
                {
                    options.Credential = new ManagedIdentityCredential(new(builder.Configuration.GetValue<string>("UserAssignedIdentityResourceId") ?? ""), new());
                }

            });
            if (!string.IsNullOrEmpty(appInsightsConfig.CloudRole))
            {
                var resourceAttributes = new Dictionary<string, object> { { "service.name", appInsightsConfig.CloudRole } };
                builder.Services.ConfigureOpenTelemetryTracerProvider((sp, b) => b.ConfigureResource(resourceBuilder => resourceBuilder.AddAttributes(resourceAttributes)));
            }
            Console.WriteLine("App Insights Running!");
        }
        else
        {
            Console.WriteLine("App Insights Not Running!");
        }
        return builder;
    }
}