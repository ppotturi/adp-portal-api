using ADP.Portal.Api.Models.Flux;
using ADP.Portal.Core.Git.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ADP.Portal.Api.Controllers;

[Route("api/[controller]")]
[ApiVersion("1.0")]
[ApiController]
public class FluxManifestController : ControllerBase
{

    private readonly IFluxManifestService fluxManifestService;
    private readonly ILogger<FluxManifestController> logger;

    public FluxManifestController(IFluxManifestService fluxManifestService, ILogger<FluxManifestController> logger)
    {
        this.fluxManifestService = fluxManifestService;
        this.logger = logger;
    }

    /// <summary>
    /// Get Patch values for Flux service template.
    /// </summary>
    /// <param name="templateType"></param>
    /// <returns></returns>
    [HttpGet("templates/service/{templateType}/patch-values")]
    [AllowAnonymous]
    public async Task<ActionResult> GetFluxServiceTemplateManifest([FromRoute] string templateType)
    {
        if (!Enum.TryParse<ServiceTemplateType>(templateType, true, out var parsedTemplateType))
        {
            return BadRequest("Invalid template type. Allowed values are 'Deploy' and 'Infra'.");
        }

        logger.LogInformation("Getting flux service template patch values for {TemplateType}", templateType);
        var result = await fluxManifestService.GetFluxServiceTemplatePatchValuesAsync(parsedTemplateType.ToString().ToLower());
        return Ok(result);
    }
}
