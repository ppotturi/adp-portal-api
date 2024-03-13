using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Common;
using Octokit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ADP.Portal.Core.Git.Services
{
    public class GitOpsFluxTeamConfigService : IGitOpsFluxTeamConfigService
    {
        private readonly IGitOpsConfigRepository gitOpsConfigRepository;
        private readonly ILogger<GitOpsFluxTeamConfigService> logger;


        public GitOpsFluxTeamConfigService(IGitOpsConfigRepository gitOpsConfigRepository, ILogger<GitOpsFluxTeamConfigService> logger)
        {
            this.gitOpsConfigRepository = gitOpsConfigRepository;
            this.logger = logger;
        }

        public async Task<GenerateFluxConfigResult> GenerateFluxTeamConfig(GitRepo gitRepo, GitRepo gitRepoFluxServices, string teamName, string? serviceName = null)
        {
            var result = new GenerateFluxConfigResult();

            logger.LogInformation("Reading flux team config for the team:'{TeamName}'.", teamName);
            var teamConfig = await GetFluxTeamConfigAsync(gitRepo, teamName);

            if (teamConfig == null)
            {
                logger.LogWarning("Flux team config not found for the team:'{TeamName}'.", teamName);
                result.IsConfigExists = false;
                return result;
            }

            logger.LogInformation("Reading flux templates");
            var templates = await gitOpsConfigRepository.GetAllFilesAsync(gitRepo, "flux/templates");

            logger.LogInformation("Processing templates");
            var generatedFiles = ProcessTemplates(templates, teamConfig, serviceName);

            if (generatedFiles.Count > 0)
            {
                var branchName = $"refs/heads/features/{teamName}" + (string.IsNullOrEmpty(serviceName) ? "" : $"-{serviceName}");
                
                logger.LogInformation("Checking if the branch:'{BranchName}' already exists.", branchName);
                var branchRef = await gitOpsConfigRepository.GetRefrenceAsync(gitRepoFluxServices, branchName);

                if (branchRef != null)
                {
                    logger.LogWarning("Flux Services feature branch: '{BranchName}' already exists.", branchName);
                    result.Errors.Add($"Flux Services feature branch: '{branchName}' already exists.");
                }
                else
                {
                    logger.LogInformation("Commit generated flux file to the branch:'{BranchName}'.", branchName);
                    
                    var message = $"Flux manifest for team({teamName}) and service({(string.IsNullOrEmpty(serviceName) ? "All" : serviceName)})";
                    var commitFiles = await gitOpsConfigRepository.CommitFilesToBranchAsync(gitRepoFluxServices, generatedFiles, branchName, message);

                    if (commitFiles)
                    {
                        logger.LogInformation("Creating pull request for the branch:'{BranchName}'.", branchName);
                        await gitOpsConfigRepository.CreatePullRequestAsync(gitRepoFluxServices, branchName, message);
                    }
                }
            }

            return result;
        }

        private async Task<FluxTeamConfig?> GetFluxTeamConfigAsync(GitRepo gitRepo, string teamName)
        {
            try
            {
                return await gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>($"flux/services/{teamName}.yaml", gitRepo);
            }
            catch (NotFoundException)
            {
                return default;
            }
        }

        private static Dictionary<string, Dictionary<object, object>> ProcessTemplates(IEnumerable<KeyValuePair<string, Dictionary<object, object>>> files, FluxTeamConfig? fluxTeamConfig, string? serviceName = null)
        {
            var finalFiles = new Dictionary<string, Dictionary<object, object>>();

            var services = (serviceName != null ? fluxTeamConfig?.Services.Where(x => x.Name.Equals(serviceName)) : fluxTeamConfig?.Services) ?? [];
            if (services.Any())
            {
                // Create service files
                finalFiles = CreateServices(files, fluxTeamConfig, services);

                // Replace tokens
                ApplyTeamTokens(fluxTeamConfig, finalFiles);
            }

            return finalFiles;
        }

        private static Dictionary<string, Dictionary<object, object>> CreateServices(IEnumerable<KeyValuePair<string, Dictionary<object, object>>> templates, FluxTeamConfig? teamConfig, IEnumerable<FluxService> services)
        {
            var finalFiles = new Dictionary<string, Dictionary<object, object>>();

            var programme = teamConfig?.ServiceCode[..3];

            // Collect all non-service files
            templates.Where(x => !x.Key.StartsWith(FluxConstants.SERVICE_FOLDER) &&
                             !x.Key.StartsWith(FluxConstants.ENVIRONMENT_FOLDER)).ForEach(file =>
            {
                var key = file.Key.Replace("flux/templates/programme", programme).Replace("team", teamConfig?.ServiceCode);
                finalFiles.Add(key, file.Value);
            });

            // Create team environments
            var envTemplates = templates.Where(x => x.Key.Contains(FluxConstants.TEAM_ENV_FOLDER));
            finalFiles.AddRange(CreateEnvironmentFiles(envTemplates, teamConfig, services));

            // Create files for each service
            var serviceTemplates = templates.Where(x => x.Key.StartsWith(FluxConstants.SERVICE_FOLDER)).ToList();
            foreach (var service in services)
            {
                var serviceFiles = new Dictionary<string, Dictionary<object, object>>();
                var serviceTypeBasedTemplates = ServiceTypeBasedFiles(serviceTemplates, service);

                foreach (var template in serviceTypeBasedTemplates)
                {
                    if (!template.Key.Contains("environment"))
                    {
                        var key = template.Key.Replace("flux/templates/programme", programme).Replace("team", teamConfig?.ServiceCode).Replace("service", service.Name);
                        serviceFiles.Add(key, template.Value);
                    }
                    else
                    {
                        serviceFiles.AddRange(CreateEnvironmentFiles([template], teamConfig, [service]));
                    }
                }
                //UpdateServiceDependencies(serviceFiles, service, teamConfig);

                service.ConfigVariables.Add(new FluxConfig { Key = "SERVICE_NAME", Value = service.Name });
                service.ConfigVariables.ForEach(serviceFiles.ReplaceToken);
                finalFiles.AddRange(serviceFiles);
            }

            return finalFiles;
        }

        private static IEnumerable<KeyValuePair<string, Dictionary<object, object>>> ServiceTypeBasedFiles(IEnumerable<KeyValuePair<string, Dictionary<object, object>>> serviceTemplates, FluxService service)
        {
            return serviceTemplates.Where(filter =>
            {
                var matched = true;
                if (IsBackendServiceWithDatabase(service))
                {
                    matched = !filter.Key.StartsWith(FluxConstants.PRE_DEPLOY_FOLDER) && !filter.Key.StartsWith(FluxConstants.PRE_DEPLOY_KUSTOMIZE_FILE);
                }
                return matched;
            });
        }

        private static bool IsBackendServiceWithDatabase(FluxService service)
        {
            return service.Type.Equals(FluxServiceType.Frontend) || !service.ConfigVariables.Exists(token => token.Key.Equals(FluxConstants.POSTGRES_DB));
        }

        private static Dictionary<string, Dictionary<object, object>> CreateEnvironmentFiles(IEnumerable<KeyValuePair<string, Dictionary<object, object>>> templates, FluxTeamConfig? teamConfig, IEnumerable<FluxService> services)
        {
            var finalFiles = new Dictionary<string, Dictionary<object, object>>();
            var programme = teamConfig?.ServiceCode[..3];

            foreach (var service in services)
            {
                foreach (var template in templates)
                {
                    service.Environments.ForEach(env =>
                    {
                        var key = template.Key.Replace("flux/templates/programme", programme).Replace("team", teamConfig?.ServiceCode).Replace("environment", $"{env[..3]}/{env[3..]}").Replace("service", service.Name);

                        if (template.Key.Equals(FluxConstants.TEAM_ENV_KUSTOMIZATION_FILE, StringComparison.InvariantCultureIgnoreCase) &&
                            finalFiles.TryGetValue(key, out var existingEnv))
                        {
                            ((List<object>)existingEnv["resources"]).Add($"../../{service.Name}");
                        }
                        else
                        {
                            var newValue = template.Value.DeepCopy();
                            if (template.Key.Equals(FluxConstants.TEAM_ENV_KUSTOMIZATION_FILE, StringComparison.InvariantCultureIgnoreCase))
                            {
                                ((List<object>)newValue["resources"]).Add($"../../{service.Name}");
                            }
                            finalFiles.Add(key, newValue);
                        }
                    });
                }
            }

            return finalFiles;
        }

        private static void UpdateServiceDependencies(Dictionary<string, Dictionary<object, object>> serviceFiles, FluxService service, FluxTeamConfig? teamConfig)
        {
            service.ConfigVariables.Add(new FluxConfig { Key = "DEPENDS_ON", Value = IsBackendServiceWithDatabase(service) ? "pre-deploy" : "infra" });
            if (service.Type.Equals(FluxServiceType.Backend))
            {
                foreach (var file in serviceFiles)
                {
                    if (file.Key.EndsWith("/patch.yaml"))
                    {
                        ((Dictionary<object, object>)((Dictionary<object, object>)file.Value["spec"])["values"]).Remove("labels");
                        ((Dictionary<object, object>)((Dictionary<object, object>)file.Value["spec"])["values"]).Remove("ingress");
                    }
                }
            }
        }

        private static void ApplyTeamTokens(FluxTeamConfig? teamConfig, Dictionary<string, Dictionary<object, object>> files)
        {
            var programme = teamConfig?.ServiceCode[..3];
            teamConfig?.ConfigVariables.Add(new FluxConfig { Key = "PROGRAMME_NAME", Value = programme ?? string.Empty });
            teamConfig?.ConfigVariables.Add(new FluxConfig { Key = "TEAM_NAME", Value = teamConfig?.ServiceCode ?? string.Empty });
            teamConfig?.ConfigVariables.Add(new FluxConfig { Key = "SERVICE_CODE", Value = teamConfig?.ServiceCode ?? string.Empty });

            teamConfig?.ConfigVariables.ForEach(files.ReplaceToken);
        }
    }

    public static partial class Extensions
    {
        const string TOKEN_FORMAT = "__{0}__";

        public static void ReplaceToken(this Dictionary<string, Dictionary<object, object>> instance, FluxConfig config)
        {
            foreach (var item in instance)
            {
                item.Value.ReplaceToken(config);
            }
        }

        public static void ReplaceToken(this Dictionary<object, object> instance, FluxConfig config)
        {
            foreach (var key in instance.Keys)
            {
                if (instance[key] is Dictionary<object, object> dictValue) { dictValue.ReplaceToken(config); }
                else if (instance[key] is List<object> listValue) { listValue.ReplaceToken(config); }
                else { instance[key] = instance[key].ToString()?.Replace(string.Format(TOKEN_FORMAT, config.Key), config.Value) ?? instance[key]; }
            }
        }

        public static void ReplaceToken(this List<object> instance, FluxConfig config)
        {
            for (int key = 0; key < instance.Count; key++)
            {
                if (instance[key] is Dictionary<object, object> value) { value.ReplaceToken(config); }
                else if (instance[key] is List<object> listValue) { listValue.ReplaceToken(config); }
                else { instance[key] = instance[key].ToString()?.Replace(string.Format(TOKEN_FORMAT, config.Key), config.Value) ?? instance[key]; }
            }
        }

        public static Dictionary<object, object> DeepCopy(this Dictionary<object, object> instance)
        {
            var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

            var serializedValue = serializer.Serialize(instance);
            return deserializer.Deserialize<Dictionary<object, object>>(serializedValue);
        }
    }
}
