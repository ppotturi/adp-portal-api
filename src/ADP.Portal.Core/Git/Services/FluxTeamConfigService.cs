using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Extensions;
using ADP.Portal.Core.Git.Infrastructure;
using ADP.Portal.Core.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Services.Common;
using Octokit;
using YamlDotNet.Serialization;

namespace ADP.Portal.Core.Git.Services
{
    public class FluxTeamConfigService : IFluxTeamConfigService
    {
        private readonly IGitHubRepository gitHubRepository;
        private readonly ICacheService cacheService;
        private readonly GitRepo teamGitRepo;
        private readonly GitRepo fluxServiceRepo;
        private readonly GitRepo fluxTemplatesRepo;
        private readonly ILogger<FluxTeamConfigService> logger;
        private readonly ISerializer serializer;

        public FluxTeamConfigService(IGitHubRepository gitHubRepository, IOptionsSnapshot<GitRepo> gitRepoOptions, ICacheService cacheService,
            ILogger<FluxTeamConfigService> logger, ISerializer serializer)
        {
            this.gitHubRepository = gitHubRepository;
            this.cacheService = cacheService;
            this.teamGitRepo = gitRepoOptions.Get(Constants.GitRepo.TEAM_REPO_CONFIG);
            this.fluxServiceRepo = gitRepoOptions.Get(Constants.GitRepo.TEAM_FLUX_SERVICES_CONFIG);
            this.fluxTemplatesRepo = gitRepoOptions.Get(Constants.GitRepo.TEAM_FLUX_TEMPLATES_CONFIG);
            this.logger = logger;
            this.serializer = serializer;
        }

        public async Task<T?> GetConfigAsync<T>(string? tenantName = null, string? teamName = null)
        {
            try
            {
                var isTenant = !string.IsNullOrEmpty(tenantName);
                var name = isTenant ? tenantName : teamName;
                var pathFormat = isTenant ? Constants.Flux.GIT_REPO_TENANT_CONFIG_PATH : Constants.Flux.GIT_REPO_TEAM_CONFIG_PATH;
                var path = string.Format(pathFormat, name);

                if (isTenant)
                {
                    logger.LogInformation("Reading flux team config for the tenant:'{TenantName}'.", name);
                }
                else
                {
                    logger.LogInformation("Reading flux team config for the team:'{TeamName}'.", name);
                }

                return await gitHubRepository.GetConfigAsync<T>(path, teamGitRepo);
            }
            catch (NotFoundException)
            {
                return default;
            }
        }

        public async Task<FluxConfigResult> CreateConfigAsync(string teamName, FluxTeamConfig fluxTeamConfig)
        {
            var result = new FluxConfigResult();

            logger.LogInformation("Creating flux team config for the team:'{TeamName}'.", teamName);

            fluxTeamConfig.Services.ForEach(service => service.Environments.ForEach(env => env.Manifest = new FluxManifest { Generate = true }));

            var response = await gitHubRepository.CreateConfigAsync(teamGitRepo, string.Format(Constants.Flux.GIT_REPO_TEAM_CONFIG_PATH, teamName), serializer.Serialize(fluxTeamConfig));
            if (string.IsNullOrEmpty(response))
            {
                result.Errors.Add($"Failed to save the config for the team: {teamName}");
            }

            return result;
        }

        public async Task<GenerateManifestResult> GenerateManifestAsync(string tenantName, string teamName, string? serviceName = null, string? environment = null)
        {
            var result = new GenerateManifestResult();

            var teamConfig = await GetConfigAsync<FluxTeamConfig>(teamName: teamName);
            var tenantConfig = await GetConfigAsync<FluxTenant>(tenantName: tenantName);

            if (teamConfig == null || tenantConfig == null)
            {
                logger.LogDebug(Constants.Logger.FLUX_TEAM_CONFIG_NOT_FOUND, teamName);
                result.IsConfigExists = false;
                return result;
            }

            logger.LogInformation("Reading flux templates.");

            var cacheKey = $"flux-templates-{fluxTemplatesRepo.Reference}";
            var templates = cacheService.Get<IEnumerable<KeyValuePair<string, FluxTemplateFile>>>(cacheKey);
            if (templates == null)
            {
                templates = await gitHubRepository.GetAllFilesAsync(fluxTemplatesRepo, Constants.Flux.GIT_REPO_TEMPLATE_PATH);
                cacheService.Set(cacheKey, templates);
            }

            logger.LogInformation("Generating flux config for the team:'{TeamName}', service:'{ServiceName}' and environment:'{Environment}'.", teamName, serviceName, environment);
            var generatedFiles = ProcessTemplates(templates, tenantConfig, teamConfig, serviceName, environment);

            if (generatedFiles.Count > 0)
            {
                await PushFilesToFluxRepository(fluxServiceRepo, teamName, serviceName, generatedFiles);
            }

            return result;
        }

        public async Task<FluxConfigResult> AddServiceAsync(string teamName, FluxService fluxService)
        {
            var result = new FluxConfigResult() { IsConfigExists = false };

            var teamConfig = await GetConfigAsync<FluxTeamConfig>(teamName: teamName);
            if (teamConfig == null)
            {
                return result;
            }

            result.IsConfigExists = true;

            if (teamConfig.Services.Exists(s => s.Name == fluxService.Name))
            {
                logger.LogDebug("Service '{ServiceName}' already exists in the team: '{TeamName}'.", fluxService.Name, teamName);
                result.Errors.Add($"Service '{fluxService.Name}' already exists in the team:'{teamName}'.");
                return result;
            }

            logger.LogInformation("Adding service '{ServiceName}' to the team:'{TeamName}'.", fluxService.Name, teamName);

            fluxService.Environments.ForEach(e => e.Manifest = new FluxManifest { Generate = true });

            teamConfig.Services.Add(fluxService);
            var response = await gitHubRepository.UpdateConfigAsync(teamGitRepo, string.Format(Constants.Flux.GIT_REPO_TEAM_CONFIG_PATH, teamName), serializer.Serialize(teamConfig));

            if (string.IsNullOrEmpty(response))
            {
                result.Errors.Add($"Failed to save the config for the team: {teamName}");
            }

            return result;
        }

        public async Task<ServiceEnvironmentResult> GetServiceEnvironmentAsync(string teamName, string serviceName, string environment)
        {
            var result = new ServiceEnvironmentResult() { IsConfigExists = false, FluxTemplatesVersion = fluxTemplatesRepo.Reference };

            var teamConfig = await GetConfigAsync<FluxTeamConfig>(teamName: teamName);
            if (teamConfig == null)
            {
                logger.LogDebug(Constants.Logger.FLUX_TEAM_CONFIG_NOT_FOUND, teamName);
                return result;
            }

            var service = teamConfig.Services.Find(s => s.Name == serviceName);
            if (service == null)
            {
                logger.LogDebug("Service '{ServiceName}' not found in the team:'{TeamName}'.", serviceName, teamName);
                return result;
            }

            var env = service.Environments.Find(e => e.Name == environment);
            if (env == null)
            {
                logger.LogDebug("Environment '{EnvironmentName}' not found for the service:'{ServiceName}' in the team:'{TeamName}'.", environment, serviceName, teamName);
                return result;
            }

            result.IsConfigExists = true;

            result.Environment = service.Environments.First(e => e.Name == environment);

            return result;
        }

        public async Task<FluxConfigResult> AddServiceEnvironmentAsync(string teamName, string serviceName, string environment)
        {
            var result = new FluxConfigResult() { IsConfigExists = false };

            var teamConfig = await GetConfigAsync<FluxTeamConfig>(teamName: teamName);
            if (teamConfig == null)
            {
                logger.LogDebug(Constants.Logger.FLUX_TEAM_CONFIG_NOT_FOUND, teamName);
                result.Errors.Add($"Flux team config not found for the team:'{teamName}'.");
                return result;
            }

            var service = teamConfig.Services.Find(s => s.Name == serviceName);
            if (service == null)
            {
                logger.LogDebug("Service '{ServiceName}' not found in the team:'{TeamName}'.", serviceName, teamName);
                result.Errors.Add($"Service '{serviceName}' not found in the team:'{teamName}'.");
                return result;
            }

            result.IsConfigExists = true;
            if (service.Environments.Exists(e => e.Name == environment))
            {
                return result;
            }

            var newEnvironment = new FluxEnvironment { Name = environment, Manifest = new FluxManifest { Generate = true } };
            service.Environments.Add(newEnvironment);

            logger.LogInformation("Adding environment '{EnvironmentName}' to the service:'{ServiceName}' in the team:'{TeamName}'.", newEnvironment.Name, serviceName, teamName);
            var response = await gitHubRepository.UpdateConfigAsync(teamGitRepo, string.Format(Constants.Flux.GIT_REPO_TEAM_CONFIG_PATH, teamName), serializer.Serialize(teamConfig));

            if (string.IsNullOrEmpty(response))
            {
                result.Errors.Add($"Failed to save the config for the team: {teamName}");
            }

            return result;
        }

        public async Task<FluxConfigResult> UpdateServiceEnvironmentManifestAsync(string teamName, string serviceName, string environment, bool generate)
        {
            var result = new FluxConfigResult() { IsConfigExists = false };

            var teamConfig = await GetConfigAsync<FluxTeamConfig>(teamName: teamName);
            if (teamConfig == null)
            {
                logger.LogDebug(Constants.Logger.FLUX_TEAM_CONFIG_NOT_FOUND, teamName);
                result.Errors.Add($"Flux team config not found for the team:'{teamName}'.");
                return result;
            }

            var service = teamConfig.Services.Find(s => s.Name == serviceName);
            if (service == null)
            {
                logger.LogDebug("Service '{ServiceName}' not found in the team:'{TeamName}'.", serviceName, teamName);
                result.Errors.Add($"Service '{serviceName}' not found in the team:'{teamName}'.");
                return result;
            }

            var env = service.Environments.Find(e => e.Name == environment);
            if (env == null)
            {
                logger.LogDebug("Environment '{EnvironmentName}' not found for the service:'{ServiceName}' in the team:'{TeamName}'.", environment, serviceName, teamName);
                result.Errors.Add($"Environment '{environment}' not found for the service:'{serviceName}' in the team:'{teamName}'.");
                return result;
            }

            result.IsConfigExists = true;

            logger.LogInformation("Updating manifest for the environment '{EnvironmentName}' for the service:'{ServiceName}' in the team:'{TeamName}'.", environment, serviceName, teamName);
            env.Manifest ??= new FluxManifest() { Generate = generate };
            env.Manifest.Generate = generate;
            env.Manifest.GeneratedVersion = fluxTemplatesRepo.Reference;
            var response = await gitHubRepository.UpdateConfigAsync(teamGitRepo, string.Format(Constants.Flux.GIT_REPO_TEAM_CONFIG_PATH, teamName), serializer.Serialize(teamConfig));

            if (string.IsNullOrEmpty(response))
            {
                result.Errors.Add($"Failed to save the config for the team: {teamName}");
            }

            return result;
        }

        #region Private methods

        private static Dictionary<string, FluxTemplateFile> ProcessTemplates(IEnumerable<KeyValuePair<string, FluxTemplateFile>> files,
            FluxTenant tenantConfig, FluxTeamConfig fluxTeamConfig, string? serviceName = null, string? environment = null)
        {
            var finalFiles = new Dictionary<string, FluxTemplateFile>();

            var services = serviceName != null ? fluxTeamConfig.Services.Where(x => x.Name.Equals(serviceName)) : fluxTeamConfig.Services;

            if (services.Any())
            {
                if (!string.IsNullOrEmpty(environment))
                {
                    FilterEnvironmentsByName(services.ToList(), environment);
                    FilterEnvironmentsByName(fluxTeamConfig.Services, environment);
                }

                // Create service files
                finalFiles = CreateServices(files, tenantConfig, fluxTeamConfig, services);

                // Replace tokens
                CreateTeamVariables(fluxTeamConfig);

                fluxTeamConfig.ConfigVariables.Union(tenantConfig.ConfigVariables).ForEach(finalFiles.ReplaceToken);
            }

            return finalFiles;
        }

        private static Dictionary<string, FluxTemplateFile> CreateServices(IEnumerable<KeyValuePair<string, FluxTemplateFile>> templates,
            FluxTenant tenantConfig, FluxTeamConfig teamConfig, IEnumerable<FluxService> services)
        {
            var finalFiles = new Dictionary<string, FluxTemplateFile>();

            // Collect all non-service files
            templates.Where(x => !x.Key.StartsWith(Constants.Flux.SERVICE_FOLDER) && !x.Key.StartsWith(Constants.Flux.TEAM_ENV_FOLDER))
                     .ForEach(file =>
                     {
                         var key = file.Key.Replace(Constants.Flux.PROGRAMME_FOLDER, teamConfig.ProgrammeName).Replace(Constants.Flux.TEAM_KEY, teamConfig.TeamName);
                         finalFiles.Add($"services/{key}", file.Value);
                     });

            // Create team environments
            var envTemplates = templates.Where(x => x.Key.Contains(Constants.Flux.TEAM_ENV_FOLDER));
            finalFiles.AddRange(CreateEnvironmentFiles(envTemplates, tenantConfig, teamConfig, teamConfig.Services));

            // Create files for each service
            var serviceTemplates = templates.Where(x => x.Key.StartsWith(Constants.Flux.SERVICE_FOLDER)).ToList();
            foreach (var service in services)
            {
                var serviceFiles = new Dictionary<string, FluxTemplateFile>();
                var serviceTypeBasedTemplates = ServiceTypeBasedFiles(serviceTemplates, service);

                foreach (var template in serviceTypeBasedTemplates)
                {
                    if (!template.Key.Contains(Constants.Flux.ENV_KEY))
                    {
                        var serviceTemplate = template.Value.DeepCopy();
                        serviceTemplate = UpdateServiceKustomizationFiles(serviceTemplate, template.Key, service);

                        var key = template.Key.Replace(Constants.Flux.PROGRAMME_FOLDER, teamConfig.ProgrammeName).Replace(Constants.Flux.TEAM_KEY, teamConfig.TeamName).Replace(Constants.Flux.SERVICE_KEY, service.Name);
                        serviceFiles.Add($"services/{key}", serviceTemplate);
                    }
                    else
                    {
                        serviceFiles.AddRange(CreateEnvironmentFiles([template], tenantConfig, teamConfig, [service]));
                    }
                }
                UpdateServicePatchFiles(serviceFiles, service, teamConfig);

                if (service.Type != FluxServiceType.HelmOnly)
                {
                    service.ConfigVariables.Add(new FluxConfig { Key = Constants.Flux.TEMPLATE_VAR_DEPENDS_ON, Value = service.HasDatastore() ? Constants.Flux.PREDEPLOY_KEY : Constants.Flux.INFRA_KEY });
                }
                service.ConfigVariables.Add(new FluxConfig { Key = Constants.Flux.TEMPLATE_VAR_SERVICE_NAME, Value = service.Name });
                service.ConfigVariables.ForEach(serviceFiles.ReplaceToken);
                finalFiles.AddRange(serviceFiles);
            }

            return finalFiles;
        }

        private static IEnumerable<KeyValuePair<string, FluxTemplateFile>> ServiceTypeBasedFiles(IEnumerable<KeyValuePair<string, FluxTemplateFile>> serviceTemplates, FluxService service)
        {
            return serviceTemplates.Where(filter =>
            {
                var matched = true;
                if (!service.HasDatastore())
                {
                    matched = !filter.Key.StartsWith(Constants.Flux.SERVICE_PRE_DEPLOY_FOLDER) && !filter.Key.StartsWith(Constants.Flux.PRE_DEPLOY_KUSTOMIZE_FILE);
                }
                return matched;
            }).Where(filter =>
            {
                var matched = true;
                if (service.Type == FluxServiceType.HelmOnly)
                {
                    matched = !filter.Key.StartsWith(Constants.Flux.SERVICE_INFRA_FOLDER) && !filter.Key.StartsWith(Constants.Flux.INFRA_KUSTOMIZE_FILE);
                }
                return matched;
            });
        }

        private static Dictionary<string, FluxTemplateFile> CreateEnvironmentFiles(IEnumerable<KeyValuePair<string, FluxTemplateFile>> templates, FluxTenant tenantConfig, FluxTeamConfig teamConfig, IEnumerable<FluxService> services)
        {
            var finalFiles = new Dictionary<string, FluxTemplateFile>();

            foreach (var service in services)
            {
                foreach (var template in templates)
                {
                    service.Environments.Where(env => tenantConfig.Environments.Exists(x => x.Name.Equals(env.Name)))
                        .ForEach(environment =>
                        {
                            var key = template.Key.Replace(Constants.Flux.PROGRAMME_FOLDER, teamConfig.ProgrammeName)
                                .Replace(Constants.Flux.TEAM_KEY, teamConfig.TeamName)
                                .Replace(Constants.Flux.ENV_KEY, $"{environment.Name[..3]}/0{environment.Name[3..]}")
                                .Replace(Constants.Flux.SERVICE_KEY, service.Name);
                            key = $"services/{key}";

                            if (template.Key.Equals(Constants.Flux.TEAM_ENV_KUSTOMIZATION_FILE, StringComparison.InvariantCultureIgnoreCase) &&
                                finalFiles.TryGetValue(key, out var existingEnv))
                            {
                                var content = existingEnv.Content[Constants.Flux.RESOURCES_KEY];
                                AddTemplateItemToList(content, $"../../{service.Name}");
                            }
                            else
                            {
                                var newFile = template.Value.DeepCopy();
                                if (template.Key.Equals(Constants.Flux.TEAM_ENV_KUSTOMIZATION_FILE, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    var content = newFile.Content[Constants.Flux.RESOURCES_KEY];
                                    AddTemplateItemToList(content, $"../../{service.Name}");
                                }

                                var tokens = new List<FluxConfig>
                            {
                                new() { Key = Constants.Flux.TEMPLATE_VAR_VERSION, Value = Constants.Flux.TEMPLATE_VAR_DEFAULT_VERSION },
                                new() { Key = Constants.Flux.TEMPLATE_VAR_VERSION_TAG, Value = Constants.Flux.TEMPLATE_VAR_DEFAULT_VERSION_TAG },
                                new() { Key = Constants.Flux.TEMPLATE_VAR_MIGRATION_VERSION, Value = Constants.Flux.TEMPLATE_VAR_DEFAULT_MIGRATION_VERSION },
                                new() { Key = Constants.Flux.TEMPLATE_VAR_MIGRATION_VERSION_TAG, Value = Constants.Flux.TEMPLATE_VAR_DEFAULT_MIGRATION_VERSION_TAG },
                                new() { Key = Constants.Flux.TEMPLATE_VAR_PS_EXEC_VERSION, Value = Constants.Flux.TEMPLATE_VAR_PS_EXEC_DEFAULT_VERSION }
                            };
                                tokens.ForEach(newFile.Content.ReplaceToken);

                                tokens =
                                [
                                    new() { Key = Constants.Flux.TEMPLATE_VAR_ENVIRONMENT, Value = environment.Name[..3]},
                                    new() { Key = Constants.Flux.TEMPLATE_VAR_ENV_INSTANCE, Value = environment.Name[3..]},
                                ];
                                var tenantConfigVariables = tenantConfig.Environments.First(x => x.Name.Equals(environment.Name)).ConfigVariables ?? [];

                                tokens.Union(environment.ConfigVariables).Union(tenantConfigVariables).ForEach(newFile.Content.ReplaceToken);
                                finalFiles.Add(key, newFile);
                            }
                        });
                }
            }
            return finalFiles;
        }

        private static FluxTemplateFile UpdateServiceKustomizationFiles(FluxTemplateFile serviceTemplate, string templateKey, FluxService service)
        {
            if (templateKey.Equals(Constants.Flux.TEAM_SERVICE_KUSTOMIZATION_FILE) && service.HasDatastore())
            {
                var content = serviceTemplate.Content[Constants.Flux.RESOURCES_KEY];
                AddTemplateItemToList(content, "pre-deploy-kustomize.yaml");
            }
            if (templateKey.Equals(Constants.Flux.TEAM_SERVICE_KUSTOMIZATION_FILE) && service.Type == FluxServiceType.HelmOnly)
            {
                var content = serviceTemplate.Content[Constants.Flux.RESOURCES_KEY];
                RemoveTemplateItemFromList(content, "infra-kustomize.yaml");
            }
            if (templateKey.Equals(Constants.Flux.DEPLOY_KUSTOMIZE_FILE) && service.Type == FluxServiceType.HelmOnly)
            {
                var content = serviceTemplate.Content[Constants.Flux.RESOURCES_KEY];
                RemoveTemplateItemFromDictionary(content, Constants.Flux.DEPENDS_ON_KEY);
            }
            return serviceTemplate;
        }

        private static void UpdateServicePatchFiles(Dictionary<string, FluxTemplateFile> serviceFiles, FluxService service, FluxTeamConfig teamConfig)
        {
            foreach (var file in serviceFiles)
            {
                service.Environments.ForEach(env =>
                {
                    var filePattern = string.Format(Constants.Flux.TEAM_SERVICE_DEPLOY_ENV_PATCH_FILE, teamConfig.ProgrammeName, teamConfig.TeamName, service.Name, $"{env.Name[..3]}/0{env.Name[3..]}");
                    if (service.Type.Equals(FluxServiceType.Backend) && file.Key.Equals(filePattern))
                    {
                        new YamlQuery(file.Value)
                            .On(Constants.Flux.SPEC_KEY)
                            .On(Constants.Flux.VALUES_KEY)
                            .Remove(Constants.Flux.LABELS_KEY)
                            .Remove(Constants.Flux.INGRESS_KEY);
                    }
                    filePattern = string.Format(Constants.Flux.TEAM_SERVICE_INFRA_ENV_PATCH_FILE, teamConfig.ProgrammeName, teamConfig.TeamName, service.Name, $"{env.Name[..3]}/0{env.Name[3..]}");
                    if (service.Type.Equals(FluxServiceType.Frontend) && file.Key.Equals(filePattern))
                    {
                        new YamlQuery(file.Value)
                            .On(Constants.Flux.SPEC_KEY)
                            .On(Constants.Flux.VALUES_KEY)
                            .Remove(Constants.Flux.POSTGRESRESOURCEGROUPNAME_KEY)
                            .Remove(Constants.Flux.POSTGRESSERVERNAME_KEY);
                    }
                });
            }
        }

        private static void CreateTeamVariables(FluxTeamConfig teamConfig)
        {
            teamConfig.ConfigVariables.Add(new FluxConfig { Key = Constants.Flux.TEMPLATE_VAR_PROGRAMME_NAME, Value = teamConfig.ProgrammeName ?? string.Empty });
            teamConfig.ConfigVariables.Add(new FluxConfig { Key = Constants.Flux.TEMPLATE_VAR_TEAM_NAME, Value = teamConfig.TeamName ?? string.Empty });
            teamConfig.ConfigVariables.Add(new FluxConfig { Key = Constants.Flux.TEMPLATE_VAR_SERVICE_CODE, Value = teamConfig.ServiceCode ?? string.Empty });
        }

        private static void FilterEnvironmentsByName(List<FluxService> services, string environment)
        {
            foreach (var service in services)
            {
                service.Environments = service.Environments.Where(env => env.Name.Equals(environment)).ToList();
            }
        }

        private async Task PushFilesToFluxRepository(GitRepo gitRepoFluxServices, string teamName, string? serviceName, Dictionary<string, FluxTemplateFile> generatedFiles)
        {
            var branchName = $"refs/heads/features/{teamName}{(string.IsNullOrEmpty(serviceName) ? "" : $"-{serviceName}")}";
            var branchRef = await gitHubRepository.GetBranchAsync(gitRepoFluxServices, branchName);

            string message;
            if (branchRef == null)
            {
                message = string.IsNullOrEmpty(serviceName) ? $"{teamName.ToUpper()} Manifest" : $"{serviceName.ToUpper()} Manifest";
            }
            else
            {
                message = "Manifest Update";
            }

            logger.LogInformation("Creating commit for the branch:'{BranchName}'.", branchName);
            var commitRef = await gitHubRepository.CreateCommitAsync(gitRepoFluxServices, generatedFiles, message, branchRef == null ? null : branchName);

            if (commitRef != null)
            {
                if (branchRef == null)
                {
                    logger.LogInformation("Creating branch:'{BranchName}'.", branchName);
                    await gitHubRepository.CreateBranchAsync(gitRepoFluxServices, branchName, commitRef.Sha);
                    logger.LogInformation("Creating pull request for the branch:'{BranchName}'.", branchName);
                    await gitHubRepository.CreatePullRequestAsync(gitRepoFluxServices, branchName, message);
                }
                else
                {
                    logger.LogInformation("Updating branch:'{BranchName}' with the changes.", branchName);
                    await gitHubRepository.UpdateBranchAsync(gitRepoFluxServices, branchName, commitRef.Sha);
                }
            }
            else
            {
                logger.LogInformation("No changes found in the flux files for the team:'{TeamName}' or the service:{ServiceName}.", teamName, serviceName);
            }
        }

        private static void AddTemplateItemToList(object content, string item)
        {
            if (content is not List<object> list)
            {
                throw new InvalidOperationException($"Unexpected type: {content.GetType()}");
            }
            else
            {
                list.Add(item);
            }
        }

        private static void RemoveTemplateItemFromList(object content, string item)
        {
            if (content is not List<object> list)
            {
                throw new InvalidOperationException($"Unexpected type: {content.GetType()}");
            }
            else
            {
                list.Remove(item);
            }
        }

        private static void RemoveTemplateItemFromDictionary(object content, string item)
        {
            if (content is not Dictionary<object, object> dictionary)
            {
                throw new InvalidOperationException($"Unexpected type: {content.GetType()}");
            }
            else
            {
                dictionary.Remove(item);
            }
        }
        #endregion
    }
}
