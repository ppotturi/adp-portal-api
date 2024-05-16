using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace ADP.Portal.Core.Git.Services;
public class FluxManifestService : IFluxManifestService
{
    private readonly IFluxTemplateService fluxTemplateService;
    private readonly ILogger<FluxManifestService> logger;

    public FluxManifestService(IFluxTemplateService fluxTemplateService, ILogger<FluxManifestService> logger)
    {
        this.fluxTemplateService = fluxTemplateService;
        this.logger = logger;
    }

    public async Task<Dictionary<object, object>?> GetFluxServiceTemplatePatchValuesAsync(string templateType)
    {
        var path = $"{Constants.Flux.Templates.SERVICE_FOLDER}/{templateType}/environment/patch.yaml";
        logger.LogDebug("Getting flux service template patch values from {Path}", path);

        var template = await fluxTemplateService.GetFluxTemplateAsync(path);
        if (template != null)
        {
            var values = new YamlQuery(template.Content).On(Constants.Flux.Templates.SPEC_KEY).Get(Constants.Flux.Templates.VALUES_KEY).ToList<Dictionary<object, object>>();
            if (values?.Count > 0)
                return values[0];
        }
        logger.LogDebug("Flux service template patch values not found for {TemplateType}", templateType);
        return default;
    }
}
