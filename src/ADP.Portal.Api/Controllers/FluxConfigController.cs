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

        [HttpGet("get/{teamName}", Name = "Get")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetConfigAsync(string teamName)
        {
            var teamRepo = teamGitRepoConfig.Value.Adapt<GitRepo>();

            var result = await gitOpsFluxTeamConfigService.GetFluxConfigAsync<FluxTeamConfig>(teamRepo, teamName: teamName);
            
            return Ok(result);
        }

        [HttpPost("create/{teamName}", Name = "Create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> CreateConfigAsync(string teamName, [FromBody] CreateFluxConfigRequest createFluxConfigRequest)
        {
            var teamRepo = teamGitRepoConfig.Value.Adapt<GitRepo>();

            var newTeamConfig = createFluxConfigRequest.Adapt<FluxTeamConfig>();

            var result = await gitOpsFluxTeamConfigService.CreateFluxConfigAsync(teamRepo, teamName, newTeamConfig);
            if (result.Errors.Count > 0) return BadRequest(result.Errors);

            return Ok();
        }

        [HttpPut("update/{teamName}", Name = "Update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateConfigAsync(string teamName, [FromBody] CreateFluxConfigRequest createFluxConfigRequest)
        {
            var teamRepo = teamGitRepoConfig.Value.Adapt<GitRepo>();

            var newTeamConfig = createFluxConfigRequest.Adapt<FluxTeamConfig>();

            var result = await gitOpsFluxTeamConfigService.UpdateFluxConfigAsync(teamRepo, teamName, newTeamConfig);

            if (!result.IsConfigExists) return BadRequest($"Flux config not found for the team:{teamName}");
            if (result.Errors.Count > 0) return BadRequest(result.Errors);

            return Ok();
        }

        [HttpPost("generate/{teamName}/{serviceName?}", Name = "Generate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GenerateAsync(string teamName, string? serviceName)
        {
            var teamRepo = teamGitRepoConfig.Value.Adapt<GitRepo>();

            var fluxServicesRepo = fluxServicesGitRepoConfig.Value.Adapt<GitRepo>();
            var tenantName = azureAdConfig.Value.TenantName;

            logger.LogInformation("Sync Flux Services for the Team:{TeamName}", teamName);
            var result = await gitOpsFluxTeamConfigService.GenerateFluxTeamConfigAsync(teamRepo, fluxServicesRepo, tenantName, teamName, serviceName);

            if (!result.IsConfigExists) return BadRequest($"Flux generator config not found for the team:{teamName}");
            if (result.Errors.Count > 0) return BadRequest(result.Errors);

            return Ok();
        }
    }
}
