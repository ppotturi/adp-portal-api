using ADP.Portal.Api.Controllers;
using ADP.Portal.Api.Models.Github;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Services;
using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace ADP.Portal.Api.Tests.Controllers;

[TestFixture]
public class GitHubTeamsControllerTests
{
    private IGitHubService github = null!;
    private GithubTeamsController sut = null!;
    private static readonly Fixture fixture = new();

    [SetUp]
    public void SetUp()
    {
        var logger = Substitute.For<ILogger<GithubTeamsController>>();
        github = Substitute.For<IGitHubService>();
        sut = new(github, logger);
    }

    [Test]
    public async Task SyncTeam_CallsTheServiceWithTheCorrectArguments()
    {
        // arrange
        using var cts = new CancellationTokenSource();
        var teamName = Guid.NewGuid().ToString();
        var request = fixture.Create<SyncTeamRequest>();
        var expected = fixture.Create<GithubTeamDetails>();

        github.SyncTeamAsync(new()
        {
            Description = request.Description,
            IsPublic = request.IsPublic,
            Maintainers = request.Maintainers,
            Members = request.Members,
            Name = teamName
        }, cts.Token).Returns(expected);

        // act
        var result = await sut.SyncTeam(teamName, request, cts.Token);

        // assert
        result.Should().BeOfType<OkObjectResult>()
            .Subject.Value.Should().BeSameAs(expected);
        _ = github.Received(1).SyncTeamAsync(new()
        {
            Description = request.Description,
            IsPublic = request.IsPublic,
            Maintainers = request.Maintainers,
            Members = request.Members,
            Name = teamName
        }, cts.Token);
    }
}