using ADP.Portal.Api.Models.Github;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace ADP.Portal.Api.Controllers;

[Route("api/github/teams")]
[ApiVersion("1.0")]
[ApiController]
public class GithubTeamsController : Controller
{
    private readonly IGitHubService github;
    private readonly ILogger<GithubTeamsController> logger;

    public GithubTeamsController(IGitHubService github, ILogger<GithubTeamsController> logger)
    {
        this.github = github;
        this.logger = logger;
    }

    [HttpPut("{teamName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(GithubTeamDetails))]
    public async Task<IActionResult> SyncTeam([FromRoute] string teamName, [FromBody] SyncTeamRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Setting github team: '{TeamName}'", teamName);
        var team = await github.SyncTeamAsync(new()
        {
            Name = teamName,
            Members = request.Members,
            Maintainers = request.Maintainers,
            Description = request.Description,
            IsPublic = request.IsPublic
        }, cancellationToken);

        return Ok(team);
    }
}