using ADP.Portal.Api.Config;
using ADP.Portal.Api.Models.Ado;
using ADP.Portal.Core.Ado.Dtos;
using ADP.Portal.Core.Ado.Entities;
using ADP.Portal.Core.Ado.Services;
using Asp.Versioning;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ADP.Portal.Api.Controllers;

[Route("api/[controller]")]
[ApiVersion("1.0")]
[ApiController]
public class AdoProjectController : ControllerBase
{
    private readonly ILogger<AdoProjectController> logger;
    private readonly IOptions<AdpAdoProjectConfig> adpAdpProjectConfig;
    private readonly IAdoProjectService adoProjectService;

    public AdoProjectController(ILogger<AdoProjectController> logger, IOptions<AdpAdoProjectConfig> adpAdpProjectConfig, IAdoProjectService adoProjectService)
    {
        this.logger = logger;
        this.adpAdpProjectConfig = adpAdpProjectConfig;
        this.adoProjectService = adoProjectService;
    }

    /// <summary>
    /// Reads details about the project from ADO organisation.
    /// </summary>
    /// <param name="projectName">Required: Name of the project</param>
    /// <returns></returns>
    [HttpGet("{projectName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetAdoProject(string projectName)
    {
        logger.LogInformation("Getting project {ProjectName}", projectName);
        var project = await adoProjectService.GetProjectAsync(projectName);
        if (project == null)
        {
            logger.LogWarning("Project {ProjectName} not found", projectName);
            return NotFound();
        }
        return Ok(project);
    }

    /// <summary>
    /// Onboards a ADO project with Environments, Agent Pool and service Connections required for ADP Platform deployments.
    /// </summary>
    /// <param name="projectName">Required: Name of the project</param>
    /// <param name="onBoardRequest">Required: Details about environments, pools, connections & variable groups</param>
    /// <returns></returns>
    [HttpPatch("{projectName}/onboard")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OnboardProjectResult))]
    public async Task<ActionResult> OnBoardAsync(string projectName, [FromBody] OnBoardAdoProjectRequest onBoardRequest)
    {
        var project = await adoProjectService.GetProjectAsync(projectName);
        if (project == null)
        {
            logger.LogWarning("Project {ProjectName} not found", projectName);
            return NotFound();
        }

        var adoProject = onBoardRequest.Adapt<AdoProject>();
        adoProject.ProjectReference = project;

        var onboardResult = await adoProjectService.OnBoardAsync(adpAdpProjectConfig.Value.Name, adoProject);

        return Ok(onboardResult);
    }
}