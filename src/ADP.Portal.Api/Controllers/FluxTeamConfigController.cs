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
    public class FluxTeamConfigController : Controller
    {
        private readonly GitRepo teamRepo;
        private readonly IGitOpsFluxTeamConfigService gitOpsFluxTeamConfigService;
        private readonly ILogger<FluxTeamConfigController> logger;
        private readonly IOptions<AzureAdConfig> azureAdConfig;
        private readonly IOptions<FluxServicesGitRepoConfig> fluxServicesGitRepoConfig;

        public FluxTeamConfigController(IGitOpsFluxTeamConfigService gitOpsFluxTeamConfigService, ILogger<FluxTeamConfigController> logger,
            IOptions<TeamGitRepoConfig> teamGitRepoConfig, IOptions<AzureAdConfig> azureAdConfig, IOptions<FluxServicesGitRepoConfig> fluxServicesGitRepoConfig)
        {
            this.gitOpsFluxTeamConfigService = gitOpsFluxTeamConfigService;
            this.logger = logger;
            this.teamRepo = teamGitRepoConfig.Value.Adapt<GitRepo>();
            this.azureAdConfig = azureAdConfig;
            this.fluxServicesGitRepoConfig = fluxServicesGitRepoConfig;
        }

        /// <summary>
        /// Read the Flux Configuration file defined for a specific team from the GitOps repository.
        /// </summary>
        /// <param name="teamName">Required: Name of the Team, like ffc-demo</param>
        /// <returns></returns>
        [HttpGet("{teamName}", Name = "GetFluxConfigForTeam")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetConfigAsync(string teamName)
        {
            logger.LogInformation("Reading Flux Config for the Team:'{TeamName}'", teamName);
            var result = await gitOpsFluxTeamConfigService.GetConfigAsync<FluxTeamConfig>(teamRepo, teamName: teamName);

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
        /// <param name="fluxConfigRequest">Required: Details about the Services, Environments & ConfigVariables for the team</param>
        /// <returns></returns>
        [HttpPost("{teamName}", Name = "CreateFluxConfigForTeam")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> CreateConfigAsync(string teamName, [FromBody] TeamFluxConfigRequest fluxConfigRequest)
        {
            var newTeamConfig = fluxConfigRequest.Adapt<FluxTeamConfig>();

            logger.LogInformation("Creating Flux Config for the Team:'{TeamName}'", teamName);
            var result = await gitOpsFluxTeamConfigService.CreateConfigAsync(teamRepo, teamName, newTeamConfig);
            if (result.Errors.Count > 0)
            {
                logger.LogError("Error while creating Flux Config for the Team:'{TeamName}'", teamName);
                return BadRequest(result.Errors);
            }

            return Created();
        }

        /// <summary>
        /// Updates the information in the Flux configuration file defined for the team in the GitOps repository.
        /// </summary>
        /// <param name="teamName">Required: Name of the Team, like ffc-demo</param>
        /// <param name="fluxConfigRequest">Required: Details about the Services, Environments & ConfigVariables for the team</param>
        /// <returns></returns>
        [HttpPut("{teamName}", Name = "UpdateFluxConfigForTeam")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> UpdateConfigAsync(string teamName, [FromBody] TeamFluxConfigRequest fluxConfigRequest)
        {

            var newTeamConfig = fluxConfigRequest.Adapt<FluxTeamConfig>();

            logger.LogInformation("Updating Flux Config for the Team:'{TeamName}'", teamName);
            var result = await gitOpsFluxTeamConfigService.UpdateConfigAsync(teamRepo, teamName, newTeamConfig);

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
        /// This operation will create a new service in the Flux Config.
        /// </summary>
        /// <param name="teamName">Required: Name of the Team, like ffc-demo</param>
        /// <param name="serviceFluxConfigRequest">The request object containing all the necessary information to create a new service in the Flux Config.</param>
        /// <returns></returns>
        [HttpPost("{teamName}/services", Name = "CreateServiceFluxConfigForTeam")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> CreateServiceAsync(string teamName, [FromBody] ServiceFluxConfigRequest serviceFluxConfigRequest)
        {
            var newTeamService = serviceFluxConfigRequest.Adapt<Core.Git.Entities.FluxService>();

            logger.LogInformation("Creating Service in the Flux Config for the Team:'{TeamName}'", teamName);

            var result = await gitOpsFluxTeamConfigService.AddFluxServiceAsync(teamRepo, teamName, newTeamService);

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

            return Created();
        }

        /// <summary>
        /// This operation will generate the Flux Manifests based on the configuration defined for the specific team in the GitOps repository.
        /// </summary>
        /// <param name="teamName">Required: Name of the Team, like ffc-demo</param>
        /// <param name="serviceName">Optional: Generate manifests only for this service if specified. Default is All services</param>
        /// <returns></returns>
        [HttpPost("{teamName}/generate", Name = "GenerateFluxConfigForTeam")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> GenerateAsync(string teamName, [FromQuery] string? serviceName)
        {
            var fluxServicesRepo = fluxServicesGitRepoConfig.Value.Adapt<GitRepo>();
            var tenantName = azureAdConfig.Value.TenantName;

            logger.LogInformation("Generating Flux Manifests for the Team:{TeamName}", teamName);
            var result = await gitOpsFluxTeamConfigService.GenerateConfigAsync(teamRepo, fluxServicesRepo, tenantName, teamName, serviceName);

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
