using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Extensions;
using ADP.Portal.Core.Git.Infrastructure;
using ADP.Portal.Core.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Common;
using Octokit;
using YamlDotNet.Serialization;

namespace ADP.Portal.Core.Git.Services
{
    public class GitOpsFluxTeamConfigService : IGitOpsFluxTeamConfigService
    {
        private readonly IGitOpsConfigRepository gitOpsConfigRepository;
        private readonly ILogger<GitOpsFluxTeamConfigService> logger;
        private readonly ISerializer serializer;

        public GitOpsFluxTeamConfigService(IGitOpsConfigRepository gitOpsConfigRepository, ILogger<GitOpsFluxTeamConfigService> logger, ISerializer serializer)
        {
            this.gitOpsConfigRepository = gitOpsConfigRepository;
            this.logger = logger;
            this.serializer = serializer;
        }

        public async Task<T?> GetConfigAsync<T>(GitRepo gitRepo, string? tenantName = null, string? teamName = null)
        {
            try
            {
                var path = string.Empty;
                if (string.IsNullOrEmpty(tenantName))
                {
                    logger.LogInformation("Reading flux team config for the team:'{TeamName}'.", teamName);
                    path = string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName);
                }
                else
                {
                    logger.LogInformation("Reading flux team config for the tenant:'{TenantName}'.", tenantName);
                    path = string.Format(FluxConstants.GIT_REPO_TENANT_CONFIG_PATH, tenantName);
                }

                return await gitOpsConfigRepository.GetConfigAsync<T>(path, gitRepo);
            }
            catch (NotFoundException)
            {
                return default;
            }
        }

        public async Task<FluxConfigResult> CreateConfigAsync(GitRepo gitRepo, string teamName, FluxTeamConfig fluxTeamConfig)
        {
            var result = new FluxConfigResult();

            logger.LogInformation("Creating flux team config for the team:'{TeamName}'.", teamName);
            var response = await gitOpsConfigRepository.CreateConfigAsync(gitRepo, string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName), serializer.Serialize(fluxTeamConfig));
            if (string.IsNullOrEmpty(response))
            {
                result.Errors.Add($"Failed to save the config for the team: {teamName}");
            }

            return result;
        }

        public async Task<FluxConfigResult> UpdateConfigAsync(GitRepo gitRepo, string teamName, FluxTeamConfig fluxTeamConfig)
        {
            var result = new FluxConfigResult() { IsConfigExists = false };

            var existingConfig = await GetConfigAsync<FluxTeamConfig>(gitRepo, teamName: teamName);
            if (existingConfig != null)
            {
                result.IsConfigExists = true;
                logger.LogInformation("Updating flux team config for the team:'{TeamName}'.", teamName);
                var response = await gitOpsConfigRepository.UpdateConfigAsync(gitRepo, string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName), serializer.Serialize(fluxTeamConfig));
                if (string.IsNullOrEmpty(response))
                {
                    result.Errors.Add($"Failed to save the config for the team: {teamName}");
                }
            }

            return result;
        }

        public async Task<FluxConfigResult> AddFluxServiceAsync(GitRepo gitRepo, string teamName, FluxService fluxService)
        {
            var result = new FluxConfigResult() { IsConfigExists = false };

            var teamConfig = await GetConfigAsync<FluxTeamConfig>(gitRepo, teamName: teamName);
            if (teamConfig != null)
            {
                result.IsConfigExists = true;
                logger.LogInformation("Adding service '{ServiceName}' to the team:'{TeamName}'.", fluxService.Name, teamName);
                if (!teamConfig.Services.Exists(s => s.Name == fluxService.Name))
                {
                    teamConfig.Services.Add(fluxService);
                    var response = await gitOpsConfigRepository.UpdateConfigAsync(gitRepo, string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName), serializer.Serialize(teamConfig));
                    if (string.IsNullOrEmpty(response))
                    {
                        result.Errors.Add($"Failed to save the config for the team: {teamName}");
                    }
                }
                else
                {
                    result.Errors.Add($"Service '{fluxService.Name}' already exists in the team:'{teamName}'.");
                    logger.LogInformation("Service '{ServiceName}' already exists in the team:'{TeamName}'.", fluxService.Name, teamName);
                }
            }

            return result;
        }

        public async Task<GenerateFluxConfigResult> GenerateConfigAsync(GitRepo gitRepo, GitRepo gitRepoFluxServices, string tenantName, string teamName, string? serviceName = null)
        {
            var result = new GenerateFluxConfigResult();

            var teamConfig = await GetConfigAsync<FluxTeamConfig>(gitRepo, teamName: teamName);
            var tenantConfig = await GetConfigAsync<FluxTenant>(gitRepo, tenantName: tenantName);

            if (teamConfig == null || tenantConfig == null)
            {
                logger.LogWarning("Flux team config not found for the team:'{TeamName}'.", teamName);
                result.IsConfigExists = false;
                return result;
            }

            logger.LogInformation("Reading flux templates");
            var templates = await gitOpsConfigRepository.GetAllFilesAsync(gitRepo, FluxConstants.GIT_REPO_TEMPLATE_PATH);

            logger.LogInformation("Processing templates");
            var generatedFiles = ProcessTemplates(templates, tenantConfig, teamConfig, serviceName);

            if (generatedFiles.Count > 0) await PushFilesToFluxRepository(gitRepoFluxServices, teamName, serviceName, generatedFiles, result);

            return result;
        }

        private static Dictionary<string, Dictionary<object, object>> ProcessTemplates(IEnumerable<KeyValuePair<string, Dictionary<object, object>>> files,
            FluxTenant tenantConfig, FluxTeamConfig fluxTeamConfig, string? serviceName = null)
        {
            var finalFiles = new Dictionary<string, Dictionary<object, object>>();

            var services = serviceName != null ? fluxTeamConfig.Services.Where(x => x.Name.Equals(serviceName)) : fluxTeamConfig.Services;
            if (services.Any())
            {
                // Create service files
                finalFiles = CreateServices(files, tenantConfig, fluxTeamConfig, services);

                // Replace tokens
                CreateTeamVariables(fluxTeamConfig);
                fluxTeamConfig.ConfigVariables.Union(tenantConfig.ConfigVariables).ForEach(finalFiles.ReplaceToken);
            }

            return finalFiles;
        }

        private static Dictionary<string, Dictionary<object, object>> CreateServices(IEnumerable<KeyValuePair<string, Dictionary<object, object>>> templates,
            FluxTenant tenantConfig, FluxTeamConfig teamConfig, IEnumerable<FluxService> services)
        {
            var finalFiles = new Dictionary<string, Dictionary<object, object>>();

            // Collect all non-service files
            templates.Where(x => !x.Key.StartsWith(FluxConstants.SERVICE_FOLDER) &&
                             !x.Key.StartsWith(FluxConstants.TEAM_ENV_FOLDER)).ForEach(file =>
            {
                var key = file.Key.Replace(FluxConstants.PROGRAMME_FOLDER, teamConfig.ProgrammeName).Replace(FluxConstants.TEAM_KEY, teamConfig.TeamName);
                finalFiles.Add(key, file.Value);
            });

            // Create team environments
            var envTemplates = templates.Where(x => x.Key.Contains(FluxConstants.TEAM_ENV_FOLDER));
            finalFiles.AddRange(CreateEnvironmentFiles(envTemplates, tenantConfig, teamConfig, teamConfig.Services));

            // Create files for each service
            var serviceTemplates = templates.Where(x => x.Key.StartsWith(FluxConstants.SERVICE_FOLDER)).ToList();
            foreach (var service in services)
            {
                var serviceFiles = new Dictionary<string, Dictionary<object, object>>();
                var serviceTypeBasedTemplates = ServiceTypeBasedFiles(serviceTemplates, service);

                foreach (var template in serviceTypeBasedTemplates)
                {
                    if (!template.Key.Contains(FluxConstants.ENV_KEY))
                    {
                        var key = template.Key.Replace(FluxConstants.PROGRAMME_FOLDER, teamConfig.ProgrammeName).Replace(FluxConstants.TEAM_KEY, teamConfig.TeamName).Replace(FluxConstants.SERVICE_KEY, service.Name);
                        serviceFiles.Add(key, template.Value.DeepCopy());
                    }
                    else
                    {
                        serviceFiles.AddRange(CreateEnvironmentFiles([template], tenantConfig, teamConfig, [service]));
                    }
                }
                UpdateServicePatchFiles(serviceFiles, service, teamConfig);

                service.ConfigVariables.Add(new FluxConfig { Key = FluxConstants.TEMPLATE_VAR_DEPENDS_ON, Value = CheckServiceConfigurationForDatabaseName(service) ? FluxConstants.PREDEPLOY_KEY : FluxConstants.INFRA_KEY });
                service.ConfigVariables.Add(new FluxConfig { Key = FluxConstants.TEMPLATE_VAR_SERVICE_NAME, Value = service.Name });
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
                if (!CheckServiceConfigurationForDatabaseName(service))
                {
                    matched = !filter.Key.StartsWith(FluxConstants.SERVICE_PRE_DEPLOY_FOLDER) && !filter.Key.StartsWith(FluxConstants.PRE_DEPLOY_KUSTOMIZE_FILE);
                }
                return matched;
            });
        }

        private static bool CheckServiceConfigurationForDatabaseName(FluxService service)
        {
            return service.ConfigVariables.Exists(token => token.Key.Equals(FluxConstants.POSTGRES_DB_KEY));
        }

        private static Dictionary<string, Dictionary<object, object>> CreateEnvironmentFiles(IEnumerable<KeyValuePair<string, Dictionary<object, object>>> templates, FluxTenant tenantConfig, FluxTeamConfig teamConfig, IEnumerable<FluxService> services)
        {
            var finalFiles = new Dictionary<string, Dictionary<object, object>>();

            foreach (var service in services)
            {
                foreach (var template in templates)
                {
                    service.Environments.Where(env => tenantConfig.Environments.Exists(x => x.Name.Equals(env.Name)))
                        .ForEach(environment =>
                    {
                        var key = template.Key.Replace(FluxConstants.PROGRAMME_FOLDER, teamConfig.ProgrammeName)
                            .Replace(FluxConstants.TEAM_KEY, teamConfig.TeamName)
                            .Replace(FluxConstants.ENV_KEY, $"{environment.Name[..3]}/0{environment.Name[3..]}")
                            .Replace(FluxConstants.SERVICE_KEY, service.Name);

                        if (template.Key.Equals(FluxConstants.TEAM_ENV_KUSTOMIZATION_FILE, StringComparison.InvariantCultureIgnoreCase) &&
                            finalFiles.TryGetValue(key, out var existingEnv))
                        {
                            ((List<object>)existingEnv[FluxConstants.RESOURCES_KEY]).Add($"../../{service.Name}");
                        }
                        else
                        {
                            var newFile = template.Value.DeepCopy();
                            if (template.Key.Equals(FluxConstants.TEAM_ENV_KUSTOMIZATION_FILE, StringComparison.InvariantCultureIgnoreCase))
                            {
                                ((List<object>)newFile[FluxConstants.RESOURCES_KEY]).Add($"../../{service.Name}");
                            }

                            var tokens = new List<FluxConfig>
                            {
                                new() { Key = FluxConstants.TEMPLATE_VAR_VERSION, Value = FluxConstants.TEMPLATE_VAR_DEFAULT_VERSION },
                                new() { Key = FluxConstants.TEMPLATE_VAR_VERSION_TAG, Value = FluxConstants.TEMPLATE_VAR_DEFAULT_VERSION_TAG },
                                new() { Key = FluxConstants.TEMPLATE_VAR_MIGRATION_VERSION, Value = FluxConstants.TEMPLATE_VAR_DEFAULT_MIGRATION_VERSION },
                                new() { Key = FluxConstants.TEMPLATE_VAR_MIGRATION_VERSION_TAG, Value = FluxConstants.TEMPLATE_VAR_DEFAULT_MIGRATION_VERSION_TAG },
                                new() { Key = FluxConstants.TEMPLATE_VAR_PS_EXEC_VERSION, Value = FluxConstants.TEMPLATE_VAR_PS_EXEC_DEFAULT_VERSION }
                            };
                            tokens.ForEach(newFile.ReplaceToken);

                            tokens =
                            [
                                new() { Key = FluxConstants.TEMPLATE_VAR_ENVIRONMENT, Value = environment.Name[..3]},
                                new() { Key = FluxConstants.TEMPLATE_VAR_ENV_INSTANCE, Value = environment.Name[3..]},
                            ];
                            var tenantConfigVariables = tenantConfig.Environments.First(x => x.Name.Equals(environment.Name)).ConfigVariables ?? [];

                            tokens.Union(environment.ConfigVariables).Union(tenantConfigVariables).ForEach(newFile.ReplaceToken);
                            finalFiles.Add(key, newFile);
                        }
                    });
                }
            }
            return finalFiles;
        }

        private static void UpdateServicePatchFiles(Dictionary<string, Dictionary<object, object>> serviceFiles, FluxService service, FluxTeamConfig teamConfig)
        {
            foreach (var file in serviceFiles)
            {
                service.Environments.ForEach(env =>
                {
                    var filePattern = string.Format(FluxConstants.TEAM_SERVICE_DEPLOY_ENV_PATCH_FILE, teamConfig.ProgrammeName, teamConfig.TeamName, service.Name, $"{env.Name[..3]}/0{env.Name[3..]}");
                    if (service.Type.Equals(FluxServiceType.Backend) && file.Key.Equals(filePattern))
                    {
                        new YamlQuery(file.Value)
                            .On(FluxConstants.SPEC_KEY)
                            .On(FluxConstants.VALUES_KEY)
                            .Remove(FluxConstants.LABELS_KEY)
                            .Remove(FluxConstants.INGRESS_KEY);
                    }
                    filePattern = string.Format(FluxConstants.TEAM_SERVICE_INFRA_ENV_PATCH_FILE, teamConfig.ProgrammeName, teamConfig.TeamName, service.Name, $"{env.Name[..3]}/0{env.Name[3..]}");
                    if (service.Type.Equals(FluxServiceType.Frontend) && file.Key.Equals(filePattern))
                    {
                        new YamlQuery(file.Value)
                            .On(FluxConstants.SPEC_KEY)
                            .On(FluxConstants.VALUES_KEY)
                            .Remove(FluxConstants.POSTGRESRESOURCEGROUPNAME_KEY)
                            .Remove(FluxConstants.POSTGRESSERVERNAME_KEY);
                    }
                });
            }
        }

        private static void CreateTeamVariables(FluxTeamConfig teamConfig)
        {
            teamConfig.ConfigVariables.Add(new FluxConfig { Key = FluxConstants.TEMPLATE_VAR_PROGRAMME_NAME, Value = teamConfig.ProgrammeName ?? string.Empty });
            teamConfig.ConfigVariables.Add(new FluxConfig { Key = FluxConstants.TEMPLATE_VAR_TEAM_NAME, Value = teamConfig.TeamName ?? string.Empty });
            teamConfig.ConfigVariables.Add(new FluxConfig { Key = FluxConstants.TEMPLATE_VAR_SERVICE_CODE, Value = teamConfig.ServiceCode ?? string.Empty });
        }

        private async Task PushFilesToFluxRepository(GitRepo gitRepoFluxServices, string teamName, string? serviceName, Dictionary<string, Dictionary<object, object>> generatedFiles, GenerateFluxConfigResult result)
        {
            var branchName = $"refs/heads/features/{teamName}{(string.IsNullOrEmpty(serviceName) ? "" : $"-{serviceName}")}";
            var branchRef = await gitOpsConfigRepository.GetBranchAsync(gitRepoFluxServices, branchName);
            var serviceDisplay = string.IsNullOrEmpty(serviceName) ? "All" : serviceName;

            var message = branchRef == null ? $"Flux config for Team:{teamName} and Service(s):{serviceDisplay}" : "Update config(s)";

            logger.LogInformation("Creating commit for the branch:'{BranchName}'.", branchName);
            var commitRef = await gitOpsConfigRepository.CreateCommitAsync(gitRepoFluxServices, generatedFiles, message, branchRef == null ? null : branchName);

            if (commitRef != null)
            {
                if (branchRef == null)
                {
                    logger.LogInformation("Creating branch:'{BranchName}'.", branchName);
                    await gitOpsConfigRepository.CreateBranchAsync(gitRepoFluxServices, branchName, commitRef.Sha);

                    logger.LogInformation("Creating pull request for the branch:'{BranchName}'.", branchName);
                    await gitOpsConfigRepository.CreatePullRequestAsync(gitRepoFluxServices, branchName, message);
                }
                else
                {
                    logger.LogInformation("Updating branch:'{BranchName}' with the changes.", branchName);
                    await gitOpsConfigRepository.UpdateBranchAsync(gitRepoFluxServices, branchName, commitRef.Sha);
                }
            }
            else
            {
                logger.LogInformation("No changes found in the flux files for the team:'{TeamName}' and service:{ServiceDisplay}.", teamName, serviceDisplay);
                result.Errors.Add($"No changes found in the flux files for the team:'{teamName}' and service:{serviceDisplay}.");
            }
        }
    }
}
