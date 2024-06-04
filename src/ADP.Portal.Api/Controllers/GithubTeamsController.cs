using ADP.Portal.Api.Models.Github;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace ADP.Portal.Api.Controllers;

[Route("api/github/teams")]
[ApiVersion("1.0")]
//[Authorize(AuthenticationSchemes = "backstage")]
[ApiController]
public class GithubTeamsController : ControllerBase
{
    private readonly IGitHubService github;
    private readonly ILogger<GithubTeamsController> logger;

    public GithubTeamsController(IGitHubService github, ILogger<GithubTeamsController> logger)
    {
        this.github = github;
        this.logger = logger;
    }

    [HttpPut("{teamId?}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(GithubTeamDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SyncTeam([FromRoute] int? teamId, [FromBody] SyncTeamRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Setting github team: '{TeamId}'", teamId);
        var team = await github.SyncTeamAsync(new()
        {
            Id = teamId,
            Name = request.Name,
            Members = request.Members,
            Maintainers = request.Maintainers,
            Description = request.Description,
            IsPublic = request.IsPublic
        }, cancellationToken);

        return team is null ? Conflict() : Ok(team);
    }
}