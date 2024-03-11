using System.Text.Json;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace ADP.Portal.Core.Git.Services
{
    public class GitOpsFluxTeamConfigService(IGitOpsConfigRepository gitOpsConfigRepository, ILogger<GitOpsFluxTeamConfigService> logger) : IGitOpsFluxTeamConfigService
    {
        private readonly IGitOpsConfigRepository gitOpsConfigRepository = gitOpsConfigRepository;
        private readonly ILogger<GitOpsFluxTeamConfigService> logger = logger;

        public async Task GenerateFluxTeamConfig(GitRepo gitRepo, string teamName, string? serviceName = null)
        {
            var teamConfig = await gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>($"flux/services/{teamName}.yaml", gitRepo);

            // Read all the Templates
            logger.LogInformation("Reading flux templates");
            var templates = await gitOpsConfigRepository.GetAllFilesAsync(gitRepo, "flux/templates");

            // Process templates with tokens
            logger.LogInformation("Processing templates");
            var generatedFiles = ProcessTemplates(templates, teamConfig, serviceName);

            // Push files to Flux Repository


            // Create a PR


            await Task.CompletedTask;
        }

        private static Dictionary<string, Dictionary<string, object>> ProcessTemplates(Dictionary<string, Dictionary<string, object>> files, FluxTeamConfig? fluxTeamConfig, string? serviceName = null)
        {
            var finalFiles = new Dictionary<string, Dictionary<string, object>>();

            var services = (serviceName != null ? fluxTeamConfig?.Services.Where(x => x.Name.Equals(serviceName)) : fluxTeamConfig?.Services) ?? [];
            if (services.Any())
            {
                // Create service files
                finalFiles = CreateServices(files, fluxTeamConfig, services);

                // Create environments
                finalFiles.AddRange(CreateEnvironments(files, fluxTeamConfig, services));

                // Replace tokens
                //fluxTeamConfig?.Tokens.ForEach(finalFiles.ReplaceToken);
            }

            return finalFiles;
        }

        private static Dictionary<string, Dictionary<string, object>> CreateServices(Dictionary<string, Dictionary<string, object>> files, FluxTeamConfig? fluxTeamConfig, IEnumerable<FluxService> services)
        {
            var finalFiles = new Dictionary<string, Dictionary<string, object>>();

            var programme = fluxTeamConfig?.ServiceCode[..3];
            fluxTeamConfig?.Tokens.Add(new FluxConfig { Key = "PROGRAMME_NAME", Value = programme ?? string.Empty });
            fluxTeamConfig?.Tokens.Add(new FluxConfig { Key = "TEAM_NAME", Value = fluxTeamConfig?.ServiceCode ?? string.Empty });
            fluxTeamConfig?.Tokens.Add(new FluxConfig { Key = "SERVICE_CODE", Value = fluxTeamConfig?.ServiceCode ?? string.Empty });

            // Collect all non-service files
            files.Where(x => !x.Key.StartsWith("flux/templates/programme/team/service") &&
                             !x.Key.StartsWith("flux/templates/programme/team/environment")).ForEach(file =>
            {
                var key = file.Key.Replace("flux/templates/programme", programme)
                                .Replace("team", fluxTeamConfig?.ServiceCode);
                finalFiles.Add(key, file.Value);
            });
            fluxTeamConfig?.Tokens.ForEach(finalFiles.ReplaceToken);

            // Create files for each service
            var serviceTemplates = files.Where(x => x.Key.StartsWith("flux/templates/programme/team/service")).ToList();
            foreach (var service in services)
            {
                var serviceFiles = new Dictionary<string, Dictionary<string, object>>();
                foreach (var file in serviceTemplates)
                {
                    var key = file.Key.Replace("flux/templates/programme", programme).Replace("team", fluxTeamConfig?.ServiceCode)
                                        .Replace("service", service.Name);
                    serviceFiles.Add(key, file.Value);
                }

                service.Tokens.Add(new FluxConfig { Key = "SERVICE_NAME", Value = service.Name });
                fluxTeamConfig?.Tokens.Union(service.Tokens).ForEach(serviceFiles.ReplaceToken);
                finalFiles.AddRange(serviceFiles);
            }
            return finalFiles;
        }

        private static Dictionary<string, Dictionary<string, object>> CreateEnvironments(Dictionary<string, Dictionary<string, object>> files, FluxTeamConfig? fluxTeamConfig, IEnumerable<FluxService> services)
        {
            var finalFiles = new Dictionary<string, Dictionary<string, object>>();
            var envFiles = files.Where(x => x.Key.StartsWith("flux/templates/programme/team/environment"));
            var programme = fluxTeamConfig?.ServiceCode[..3];

            foreach (var service in services)
            {
                foreach (var envFile in envFiles)
                {
                    service.Environments.ForEach(env =>
                    {
                        var key = envFile.Key.Replace("flux/templates/programme", programme).Replace("team", fluxTeamConfig?.ServiceCode).Replace("environment", $"{env[..3]}/{env[3..]}");
                        if (finalFiles.TryGetValue(key, out var existingEnv))
                        {
                            ((List<object>)existingEnv["resources"]).Add($"../../{service.Name}");
                        }
                        else
                        {
                            var newValue = DeepCopy(envFile.Value);
                            ((List<object>)newValue["resources"]).Add($"../../{service.Name}");
                            finalFiles.Add(key, newValue);
                        }
                    });
                }
            }

            return finalFiles;
        }

        public static Dictionary<string, object> DeepCopy(Dictionary<string, object> original)
        {
            var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

            var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

            string json = serializer.Serialize(original);
            return deserializer.Deserialize<Dictionary<string, object>>(json);
        }
    }

    public static partial class Extensions
    {
        const string TOKEN_FORMAT = "__{0}__";

        public static void ReplaceToken(this Dictionary<string, Dictionary<string, object>> instance, FluxConfig config)
        {
            foreach (var item in instance)
            {
                item.Value.ReplaceToken(config);
            }
        }

        public static void ReplaceToken(this Dictionary<string, object> instance, FluxConfig config)
        {
            foreach (var key in instance.Keys)
            {
                if (instance[key] is Dictionary<string, object> value)
                {
                    value.ReplaceToken(config);
                }
                else
                {
                    instance[key] = instance[key].ToString()?.Replace(string.Format(TOKEN_FORMAT, config.Key), config.Value) ?? instance[key];
                }
            }
        }
    }
}
