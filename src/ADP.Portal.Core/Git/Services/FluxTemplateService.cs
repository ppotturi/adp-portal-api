using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ADP.Portal.Core.Git.Services;
public class FluxTemplateService : IFluxTemplateService
{
    private readonly IGitHubRepository gitHubRepository;
    private readonly ICacheService cacheService;
    private readonly ILogger<FluxTemplateService> logger;
    private readonly GitRepo fluxTemplatesRepo;
    public FluxTemplateService(IGitHubRepository gitHubRepository, IOptionsSnapshot<GitRepo> gitRepoOptions, ICacheService cacheService, ILogger<FluxTemplateService> logger)
    {
        this.gitHubRepository = gitHubRepository;
        this.cacheService = cacheService;
        this.logger = logger;
        this.fluxTemplatesRepo = gitRepoOptions.Get(Constants.GitRepo.TEAM_FLUX_TEMPLATES_CONFIG);
    }

    public async Task<IEnumerable<KeyValuePair<string, FluxTemplateFile>>> GetFluxTemplatesAsync()
    {
        var cacheKey = $"flux-templates-{fluxTemplatesRepo.Reference}";

        logger.LogDebug("Getting flux templates from cache");
        var templates = cacheService.Get<IEnumerable<KeyValuePair<string, FluxTemplateFile>>>(cacheKey);
        if (templates == null)
        {
            logger.LogDebug("Getting flux templates from GitHub");
            templates = await gitHubRepository.GetAllFilesAsync(fluxTemplatesRepo, Constants.Flux.Templates.GIT_REPO_TEMPLATE_PATH);

            logger.LogDebug("Caching flux templates");
            cacheService.Set(cacheKey, templates);
        }

        return templates;
    }

    public async Task<FluxTemplateFile?> GetFluxTemplateAsync(string path)
    {
        var templates = await GetFluxTemplatesAsync();
        return templates.FirstOrDefault(t => t.Key == path).Value;
    }
}
