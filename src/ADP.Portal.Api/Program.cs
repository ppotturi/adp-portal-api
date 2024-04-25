using ADP.Portal.Api.Config;
using ADP.Portal.Api.Mapster;
using ADP.Portal.Api.Providers;
using ADP.Portal.Api.Swagger;
using ADP.Portal.Api.Wrappers;
using ADP.Portal.Core.Ado.Infrastructure;
using ADP.Portal.Core.Ado.Services;
using ADP.Portal.Core.Azure.Infrastructure;
using ADP.Portal.Core.Azure.Services;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using ADP.Portal.Core.Git.Jwt;
using ADP.Portal.Core.Git.Services;
using Asp.Versioning;
using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.OpenApi.Models;
using Octokit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ADP.Portal.Api
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            ConfigureApp(builder);
            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            if (app.Environment.IsProduction())
            {
                app.UseExceptionHandler();
            }
            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        private static readonly string[] graphApiDefaultScope = [".default"];

        public static void ConfigureApp(WebApplicationBuilder builder)
        {
            builder.Services.AddLogging();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();
            builder.Services.Configure<AdoConfig>(builder.Configuration.GetSection("Ado"));
            builder.Services.Configure<AdpAdoProjectConfig>(builder.Configuration.GetSection("AdpAdoProject"));
            builder.Services.Configure<AzureAdConfig>(builder.Configuration.GetSection("AzureAd"));
            builder.Services.Configure<GitHubAppAuthConfig>(builder.Configuration.GetSection("GitHubAppAuth"));
            builder.Services.Configure<GitHubOptions>(builder.Configuration.GetSection("TeamGitRepo"));
            builder.Services.Configure<TeamGitRepoConfig>(builder.Configuration.GetSection("TeamGitRepo"));
            builder.Services.Configure<FluxServicesGitRepoConfig>(builder.Configuration.GetSection("FluxServicesGitRepo"));
            builder.Services.AddScoped<IAzureCredential>(provider =>
            {
                return new DefaultAzureCredentialWrapper();
            });
            builder.Services.AddScoped(async provider =>
            {
                var azureCredentialsService = provider.GetRequiredService<IAzureCredential>();
                var adoAzureAdConfig = provider.GetRequiredService<IOptions<AdoConfig>>().Value;
                var vssConnectionProvider = new VssConnectionProvider(azureCredentialsService, adoAzureAdConfig);
                var connection = await vssConnectionProvider.GetConnectionAsync();
                return connection;
            });

            builder.Services.AddScoped<IAdoRestApiService, AdoRestApiService>();
            builder.Services.AddScoped<IAdoProjectService, AdoProjectService>();
            builder.Services.AddScoped<IAdoService, AdoService>();
            builder.Services.AddScoped<IGroupService, GroupService>();
            builder.Services.AddScoped<IAzureAadGroupService, AzureAadGroupService>();
            builder.Services.AddScoped(provider =>
            {
                var azureAdConfig = provider.GetRequiredService<IOptions<AzureAdConfig>>().Value;
                var clientSecretCredential = new ClientSecretCredential(azureAdConfig.TenantId, azureAdConfig.SpClientId, azureAdConfig.SpClientSecret);

                var graphBaseUrl = "https://graph.microsoft.com/v1.0";

                return new GraphServiceClient(clientSecretCredential, graphApiDefaultScope, graphBaseUrl);
            });

            builder.Services.AddScoped<IGitHubClient>(provider =>
            {
                var gitHubAppAuth = provider.GetRequiredService<IOptions<GitHubAppAuthConfig>>().Value;
                return GetGitHubClient(gitHubAppAuth);
            });

            builder.Services.AddScoped<IGitHubService, GitHubService>();
            builder.Services.AddScoped<IGitOpsConfigRepository, GitOpsConfigRepository>();
            builder.Services.AddScoped<IGitOpsGroupsConfigService, GitOpsGroupsConfigService>();
            builder.Services.AddScoped<IGitOpsFluxTeamConfigService, GitOpsFluxTeamConfigService>();
            builder.Services.AddSingleton(provider =>
            {
                return new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            });
            builder.Services.AddSingleton(provider =>
            {
                return new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            });
            builder.Services.Configure();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Adp.Portal.Api", Version = "v1" });
                c.OperationFilter<OptionalPathParameterOperationFilter>();
            });
            builder.Services.AddApiVersioning(config =>
            {
                config.DefaultApiVersion = new ApiVersion(1, 0);
                config.AssumeDefaultVersionWhenUnspecified = true;
                config.ReportApiVersions = true;
                config.ApiVersionReader = new HeaderApiVersionReader("api-version");
            });
        }

        private static GitHubClient GetGitHubClient(GitHubAppAuthConfig gitHubAppAuth)
        {
            var gitHubAppName = gitHubAppAuth.AppName.Replace(" ", "");

            var appClient = new GitHubClient(new ProductHeaderValue(gitHubAppName))
            {
                Credentials = new Credentials(JwtTokenHelper.CreateEncodedJwtToken(gitHubAppAuth.PrivateKeyBase64, gitHubAppAuth.AppId), AuthenticationType.Bearer)
            };

            var installations = appClient.GitHubApps.GetAllInstallationsForCurrent().Result;

            var instationId = installations.First(i => i.Account.Login.Equals(gitHubAppAuth.Owner, StringComparison.CurrentCultureIgnoreCase)).Id;

            var response = appClient.GitHubApps.CreateInstallationToken(instationId).Result;

            return new GitHubClient(new ProductHeaderValue($"{gitHubAppName}-{instationId}"))
            {
                Credentials = new Credentials(response.Token)
            };
        }
    }
}