using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using ADP.Portal.Core.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Common;
using Octokit;

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

        public async Task<GenerateFluxConfigResult> GenerateFluxTeamConfig(GitRepo gitRepo, GitRepo gitRepoFluxServices, string tenantName, string teamName, string? serviceName = null)
        {
            var result = new GenerateFluxConfigResult();

            logger.LogInformation("Reading flux team config for the team:'{TeamName}'.", teamName);
            var teamConfig = await GetFluxConfigAsync<FluxTeamConfig>(gitRepo, teamName: teamName);
            var tenantConfig = await GetFluxConfigAsync<FluxTenant>(gitRepo, tenantName: tenantName);

            if (teamConfig == null)
            {
                logger.LogWarning("Flux team config not found for the team:'{TeamName}'.", teamName);
                result.IsConfigExists = false;
                return result;
            }

            logger.LogInformation("Reading flux templates");
            var templates = await gitOpsConfigRepository.GetAllFilesAsync(gitRepo, "flux/templates");

            logger.LogInformation("Processing templates");
            var generatedFiles = ProcessTemplates(templates, tenantConfig, teamConfig, serviceName);

            if (generatedFiles.Count > 0)
            {
                var branchName = $"refs/heads/features/{teamName}" + (string.IsNullOrEmpty(serviceName) ? "" : $"-{serviceName}");
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
                    result.Errors.Add($"No changes found in the flux files for the team:'{teamName}' and service:{serviceDisplay} .");
                }
            }

            return result;
        }

        private async Task<T?> GetFluxConfigAsync<T>(GitRepo gitRepo, string? tenantName = null, string? teamName = null)
        {
            try
            {
                var path = !string.IsNullOrEmpty(tenantName) ? $"flux/{tenantName}-config.yaml" : $"flux/services/{teamName}.yaml";
                return await gitOpsConfigRepository.GetConfigAsync<T>(path, gitRepo);
            }
            catch (NotFoundException)
            {
                return default;
            }
        }

        private static Dictionary<string, Dictionary<object, object>> ProcessTemplates(IEnumerable<KeyValuePair<string, Dictionary<object, object>>> files,
            FluxTenant? tenantConfig, FluxTeamConfig? fluxTeamConfig, string? serviceName = null)
        {
            var finalFiles = new Dictionary<string, Dictionary<object, object>>();

            var services = (serviceName != null ? fluxTeamConfig?.Services.Where(x => x.Name.Equals(serviceName)) : fluxTeamConfig?.Services) ?? [];
            if (services.Any())
            {
                // Create service files
                finalFiles = CreateServices(files, tenantConfig, fluxTeamConfig, services);

                // Replace tokens
                ApplyTeamTokens(fluxTeamConfig, tenantConfig, finalFiles);
            }

            return finalFiles;
        }

        private static Dictionary<string, Dictionary<object, object>> CreateServices(IEnumerable<KeyValuePair<string, Dictionary<object, object>>> templates, 
            FluxTenant? tenantConfig, FluxTeamConfig? teamConfig, IEnumerable<FluxService> services)
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
            finalFiles.AddRange(CreateEnvironmentFiles(envTemplates, tenantConfig, teamConfig, services));

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
                        serviceFiles.AddRange(CreateEnvironmentFiles([template], tenantConfig, teamConfig, [service]));
                    }
                }
                UpdateServiceDependencies(serviceFiles, service, teamConfig);

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

        private static Dictionary<string, Dictionary<object, object>> CreateEnvironmentFiles(IEnumerable<KeyValuePair<string, Dictionary<object, object>>> templates, FluxTenant? tenantConfig, FluxTeamConfig? teamConfig, IEnumerable<FluxService> services)
        {
            var finalFiles = new Dictionary<string, Dictionary<object, object>>();
            var programme = teamConfig?.ServiceCode[..3];

            foreach (var service in services)
            {
                foreach (var template in templates)
                {
                    service.Environments.Where(env => tenantConfig != null ? tenantConfig.Environments.Exists(x => x.Name.Equals(env.Name)) : true)
                        .ForEach(env =>
                    {
                        var key = template.Key.Replace("flux/templates/programme", programme).Replace("team", teamConfig?.ServiceCode).Replace("environment", $"{env.Name[..3]}/0{env.Name[3..]}").Replace("service", service.Name);

                        if (template.Key.Equals(FluxConstants.TEAM_ENV_KUSTOMIZATION_FILE, StringComparison.InvariantCultureIgnoreCase) &&
                            finalFiles.TryGetValue(key, out var existingEnv))
                        {
                            ((List<object>)existingEnv["resources"]).Add($"../../{service.Name}");
                        }
                        else
                        {
                            var newFile = template.Value.DeepCopy();
                            if (template.Key.Equals(FluxConstants.TEAM_ENV_KUSTOMIZATION_FILE, StringComparison.InvariantCultureIgnoreCase))
                            {
                                ((List<object>)newFile["resources"]).Add($"../../{service.Name}");
                            }
                            var tokens = new List<FluxConfig>
                            {
                                new() { Key = "ENVIRONMENT", Value = env.Name[..3]},
                                new() { Key = "ENV_INSTANCE", Value = env.Name[3..]}
                            };
                            tokens.Union(env.ConfigVariables).ForEach(newFile.ReplaceToken);
                            finalFiles.Add(key, newFile);
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
                    service.Environments.ForEach(env =>
                    {
                        var filePattern = string.Format(FluxConstants.TEAM_SERVICE_ENV_PATCH_FILE, teamConfig?.ServiceCode[..3], teamConfig?.ServiceCode, service.Name, $"{env.Name[..3]}/0{env.Name[3..]}");
                        if (file.Key.Equals(filePattern))
                        {
                            new YamlQuery(file.Value)
                                .On("spec")
                                .On("values")
                                .Remove("labels")
                                .Remove("ingress");
                        }
                    });
                }
            }
        }

        private static void ApplyTeamTokens(FluxTeamConfig? teamConfig, FluxTenant? fluxTenant, Dictionary<string, Dictionary<object, object>> files)
        {
            var programme = teamConfig?.ServiceCode[..3];
            teamConfig?.ConfigVariables.Add(new FluxConfig { Key = "PROGRAMME_NAME", Value = programme ?? string.Empty });
            teamConfig?.ConfigVariables.Add(new FluxConfig { Key = "TEAM_NAME", Value = teamConfig?.ServiceCode ?? string.Empty });
            teamConfig?.ConfigVariables.Add(new FluxConfig { Key = "SERVICE_CODE", Value = teamConfig?.ServiceCode ?? string.Empty });
            teamConfig?.ConfigVariables.Add(new FluxConfig { Key = "VERSION", Value = "0.1.0" });

            teamConfig?.ConfigVariables.Union(fluxTenant?.ConfigVariables ?? []).ForEach(files.ReplaceToken);
        }
    }
}
