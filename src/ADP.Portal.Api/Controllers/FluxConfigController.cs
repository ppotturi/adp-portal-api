using ADP.Portal.Api.Config;
using ADP.Portal.Api.Models.Flux;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Services;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ADP.Portal.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiVersion("1")]
    [ApiController]
    public class FluxConfigController : Controller
    {
        private readonly IGitOpsFluxTeamConfigService gitOpsFluxTeamConfigService;
        private readonly ILogger<FluxConfigController> logger;
        private readonly IOptions<TeamGitRepoConfig> teamGitRepoConfig;
        public readonly IOptions<AzureAdConfig> azureAdConfig;
        private readonly IOptions<FluxServicesGitRepoConfig> fluxServicesGitRepoConfig;

        public FluxConfigController(IGitOpsFluxTeamConfigService gitOpsFluxTeamConfigService, ILogger<FluxConfigController> logger,
            IOptions<TeamGitRepoConfig> teamGitRepoConfig, IOptions<AzureAdConfig> azureAdConfig, IOptions<FluxServicesGitRepoConfig> fluxServicesGitRepoConfig)
        {
            this.gitOpsFluxTeamConfigService = gitOpsFluxTeamConfigService;
            this.logger = logger;
            this.teamGitRepoConfig = teamGitRepoConfig;
            this.azureAdConfig = azureAdConfig;
            this.fluxServicesGitRepoConfig = fluxServicesGitRepoConfig;
        }

        /// <summary>
        /// Read the Flux Configuration file defined for a specific team from the GitOps repository.
        /// </summary>
        /// <param name="teamName">Required: Name of the Team, like ffc-demo</param>
        /// <returns></returns>
        [HttpGet("get/{teamName}", Name = "Get")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetConfigAsync(string teamName)
        {
            var teamRepo = teamGitRepoConfig.Value.Adapt<GitRepo>();

            logger.LogInformation("Reading Flux Config for the Team:'{TeamName}'", teamName);
            var result = await gitOpsFluxTeamConfigService.GetFluxConfigAsync<FluxTeamConfig>(teamRepo, teamName: teamName);

            if (result != null)
            {
                return Ok(result);
            }

            return NotFound();
        }

        /// <summary>
        /// Create a new Flux configuration file in the GitOps repository for a specific team.
        /// </summary>
        /// <param name="teamName">Required: Name of the Team, like ffc-demo</param>
        /// <param name="createFluxConfigRequest">Required: Details about the Services, Environments & ConfigVariables for the team</param>
        /// <returns></returns>
        [HttpPost("create/{teamName}", Name = "Create")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> CreateConfigAsync(string teamName, [FromBody] CreateFluxConfigRequest createFluxConfigRequest)
        {
            var teamRepo = teamGitRepoConfig.Value.Adapt<GitRepo>();

            var newTeamConfig = createFluxConfigRequest.Adapt<FluxTeamConfig>();

            logger.LogInformation("Creating Flux Config for the Team:'{TeamName}'", teamName);
            var result = await gitOpsFluxTeamConfigService.CreateFluxConfigAsync(teamRepo, teamName, newTeamConfig);
            if (result.Errors.Count > 0)
            {
                logger.LogError("Error while creating Flux Config for the Team:'{TeamName}'", teamName);
                return BadRequest(result.Errors);
            }

            return NoContent();
        }

        /// <summary>
        /// Updates the information in the Flux configuration file defined for the team in the GitOps repository.
        /// </summary>
        /// <param name="teamName">Required: Name of the Team, like ffc-demo</param>
        /// <param name="createFluxConfigRequest">Required: Details about the Services, Environments & ConfigVariables for the team</param>
        /// <returns></returns>
        [HttpPut("update/{teamName}", Name = "Update")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> UpdateConfigAsync(string teamName, [FromBody] CreateFluxConfigRequest createFluxConfigRequest)
        {
            var teamRepo = teamGitRepoConfig.Value.Adapt<GitRepo>();

            var newTeamConfig = createFluxConfigRequest.Adapt<FluxTeamConfig>();

            logger.LogInformation("Updating Flux Config for the Team:'{TeamName}'", teamName);
            var result = await gitOpsFluxTeamConfigService.UpdateFluxConfigAsync(teamRepo, teamName, newTeamConfig);

            if (!result.IsConfigExists)
            {
                logger.LogWarning("Flux Config not found for the Team:'{TeamName}'", teamName);
                return BadRequest($"Flux config not found for the team:{teamName}");
            }
            if (result.Errors.Count > 0)
            {
                logger.LogError("Error while updating Flux config for the Team:'{TeamName}'", teamName);
                return BadRequest(result.Errors);
            }

            return NoContent();
        }

        /// <summary>
        /// This operation will generate the Flux Manifests based on the configuration defined for the specific team in the GitOps repository.
        /// </summary>
        /// <param name="teamName">Required: Name of the Team, like ffc-demo</param>
        /// <param name="serviceName">Optional: Generate manifests only for this service if specified. Default is All services</param>
        /// <returns></returns>
        [HttpPost("generate/{teamName}/{serviceName?}", Name = "Generate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> GenerateAsync(string teamName, string? serviceName)
        {
            var teamRepo = teamGitRepoConfig.Value.Adapt<GitRepo>();

            var fluxServicesRepo = fluxServicesGitRepoConfig.Value.Adapt<GitRepo>();
            var tenantName = azureAdConfig.Value.TenantName;

            logger.LogInformation("Generating Flux Manifests for the Team:{TeamName}", teamName);
            var result = await gitOpsFluxTeamConfigService.GenerateFluxTeamConfigAsync(teamRepo, fluxServicesRepo, tenantName, teamName, serviceName);

            if (!result.IsConfigExists)
            {
                logger.LogWarning("Flux Config not found for the Team:'{TeamName}'", teamName);
                return BadRequest($"Flux generator config not found for the team:{teamName}");
            }
            if (result.Errors.Count > 0)
            {
                logger.LogError("Error while generating manifests for the Team:'{TeamName}'", teamName);
                return BadRequest(result.Errors);
            }

            return NoContent();
        }
    }
}
