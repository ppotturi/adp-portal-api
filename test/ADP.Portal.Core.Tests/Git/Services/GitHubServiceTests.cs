using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Services;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Octokit;
using System.Net;

namespace ADP.Portal.Core.Tests.Git.Services;

[TestFixture]
public class GitHubServiceTests
{
    private GitHubOptions options = null!;
    private IGitHubClient client = null!;
    private GitHubService sut = null!;
    private static readonly Fixture fixture = new();

    [SetUp]
    public void SetUp()
    {
        var logger = Substitute.For<ILogger<GitHubService>>();
        options = new()
        {
            Organisation = Guid.NewGuid().ToString()
        };
        client = Substitute.For<IGitHubClient>();
        sut = new GitHubService(client, Options.Create(options), logger);
    }

    [Test]
    [TestCase(true, TeamPrivacy.Closed)]
    [TestCase(false, TeamPrivacy.Secret)]
    public async Task SyncTeamAsync_CreatesATeamThatDoesntAlreadyExist(bool isPublic, TeamPrivacy privacy)
    {
        // arrange
        using var cts = new CancellationTokenSource();
        var request = fixture.Create<GithubTeamUpdate>();
        request.IsPublic = isPublic;
        request.Maintainers = fixture.CreateMany<string>(3).ToArray();
        request.Members = fixture.CreateMany<string>(3).ToArray();

        var team = CreateTeam(privacy: privacy);

        client.Organization.Team.GetByName(options.Organisation, request.Name)
            .ThrowsAsync(new NotFoundException("TESTING", HttpStatusCode.NotFound));

        client.Organization.Team.Create(default, default)
            .ReturnsForAnyArgs(team);

        // act
        var actual = await sut.SyncTeamAsync(request, cts.Token);

        // assert
        _ = client.Organization.Team.Received(1).Create(options.Organisation, Arg.Is<NewTeam>(t =>
            t.Name == request.Name
            && t.Description == request.Description
            && t.Privacy == privacy
            && t.Maintainers.SequenceEqual(request.Maintainers)));

        _ = client.Organization.Team.Received(1).AddOrEditMembership(team.Id, request.Members.ElementAt(0), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));
        _ = client.Organization.Team.Received(1).AddOrEditMembership(team.Id, request.Members.ElementAt(1), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));
        _ = client.Organization.Team.Received(1).AddOrEditMembership(team.Id, request.Members.ElementAt(2), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));

        actual.Should().BeEquivalentTo(new GithubTeamDetails()
        {
            Description = team.Description,
            Id = team.Id,
            IsPublic = isPublic,
            Name = team.Name,
            Slug = team.Slug,
            Maintainers = request.Maintainers,
            Members = request.Members
        });
    }

    [Test]
    [TestCase(true, TeamPrivacy.Closed)]
    [TestCase(false, TeamPrivacy.Secret)]
    public async Task SyncTeamAsync_UpdatesATeamThatAlreadyExists(bool isPublic, TeamPrivacy privacy)
    {
        // arrange
        using var cts = new CancellationTokenSource();
        var request = fixture.Create<GithubTeamUpdate>();
        var currentMaintainers = fixture.CreateMany<string>(3).ToArray();
        var currentMembers = fixture.CreateMany<string>(3).ToArray();
        request.IsPublic = isPublic;
        request.Maintainers = fixture.CreateMany<string>(2).Concat(currentMaintainers.Take(1)).ToArray();
        request.Members = fixture.CreateMany<string>(2).Concat(currentMembers.Take(1)).ToArray();

        var team = CreateTeam(privacy: privacy);

        client.Organization.Team.GetByName(options.Organisation, request.Name)
            .Returns(team);
        client.Organization.Team.GetAllMembers(team.Id, Arg.Is<TeamMembersRequest>(t =>
            t.Role == TeamRoleFilter.Member))
            .Returns(currentMembers.Select(CreateUserWithLogin).ToList());
        client.Organization.Team.GetAllMembers(team.Id, Arg.Is<TeamMembersRequest>(t =>
            t.Role == TeamRoleFilter.Maintainer))
            .Returns(currentMaintainers.Select(CreateUserWithLogin).ToList());
        client.Organization.Team.Update(team.Id, Arg.Is<UpdateTeam>(t =>
            t.Name == request.Name
            && t.Description == request.Description
            && t.Privacy == privacy))
            .Returns(team);

        // act
        var actual = await sut.SyncTeamAsync(request, cts.Token);

        // assert
        _ = client.Organization.Team.Received(1).Update(team.Id, Arg.Is<UpdateTeam>(t =>
            t.Name == request.Name
            && t.Description == request.Description
            && t.Privacy == privacy));

        _ = client.Organization.Team.Received(1).AddOrEditMembership(team.Id, request.Members.ElementAt(0), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));
        _ = client.Organization.Team.Received(1).AddOrEditMembership(team.Id, request.Members.ElementAt(1), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));
        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Members.ElementAt(2), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));
        _ = client.Organization.Team.Received(0).RemoveMembership(team.Id, currentMembers[0]);
        _ = client.Organization.Team.Received(1).RemoveMembership(team.Id, currentMembers[1]);
        _ = client.Organization.Team.Received(1).RemoveMembership(team.Id, currentMembers[2]);
        _ = client.Organization.Team.Received(1).AddOrEditMembership(team.Id, request.Maintainers.ElementAt(0), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Maintainer));
        _ = client.Organization.Team.Received(1).AddOrEditMembership(team.Id, request.Maintainers.ElementAt(1), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Maintainer));
        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Maintainers.ElementAt(2), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Maintainer));
        _ = client.Organization.Team.Received(0).RemoveMembership(team.Id, currentMaintainers[0]);
        _ = client.Organization.Team.Received(1).RemoveMembership(team.Id, currentMaintainers[1]);
        _ = client.Organization.Team.Received(1).RemoveMembership(team.Id, currentMaintainers[2]);

        actual.Should().BeEquivalentTo(new GithubTeamDetails()
        {
            Description = team.Description,
            Id = team.Id,
            IsPublic = isPublic,
            Name = team.Name,
            Slug = team.Slug,
            Maintainers = request.Maintainers,
            Members = request.Members
        });

        static User CreateUserWithLogin(string login)
            => new(default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, login: login, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!);
    }

    [Test]
    [TestCase(true, TeamPrivacy.Closed)]
    [TestCase(false, TeamPrivacy.Secret)]
    public async Task SyncTeamAsync_DoesNothingIfThereAreNoChanges(bool isPublic, TeamPrivacy privacy)
    {
        // arrange
        using var cts = new CancellationTokenSource();
        var request = fixture.Create<GithubTeamUpdate>();
        request.IsPublic = isPublic;
        request.Maintainers = fixture.CreateMany<string>(3).ToArray();
        request.Members = fixture.CreateMany<string>(3).ToArray();

        var team = CreateTeam(
            description: request.Description,
            privacy: privacy,
            name: request.Name);

        client.Organization.Team.GetByName(options.Organisation, request.Name)
            .Returns(team);
        client.Organization.Team.GetAllMembers(team.Id, Arg.Is<TeamMembersRequest>(t =>
            t.Role == TeamRoleFilter.Member))
            .Returns(request.Members.Select(CreateUserWithLogin).ToList());
        client.Organization.Team.GetAllMembers(team.Id, Arg.Is<TeamMembersRequest>(t =>
            t.Role == TeamRoleFilter.Maintainer))
            .Returns(request.Maintainers.Select(CreateUserWithLogin).ToList());

        // act
        var actual = await sut.SyncTeamAsync(request, cts.Token);

        // assert
        _ = client.Organization.Team.Received(0).Update(team.Id, Arg.Is<UpdateTeam>(t =>
            t.Name == request.Name
            && t.Description == request.Description
            && t.Privacy == privacy));

        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Members.ElementAt(0), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));
        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Members.ElementAt(1), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));
        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Members.ElementAt(2), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));
        _ = client.Organization.Team.Received(0).RemoveMembership(team.Id, request.Members.ElementAt(0));
        _ = client.Organization.Team.Received(0).RemoveMembership(team.Id, request.Members.ElementAt(1));
        _ = client.Organization.Team.Received(0).RemoveMembership(team.Id, request.Members.ElementAt(2));
        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Maintainers.ElementAt(0), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Maintainer));
        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Maintainers.ElementAt(1), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Maintainer));
        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Maintainers.ElementAt(2), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Maintainer));
        _ = client.Organization.Team.Received(0).RemoveMembership(team.Id, request.Maintainers.ElementAt(0));
        _ = client.Organization.Team.Received(0).RemoveMembership(team.Id, request.Maintainers.ElementAt(1));
        _ = client.Organization.Team.Received(0).RemoveMembership(team.Id, request.Maintainers.ElementAt(2));

        actual.Should().BeEquivalentTo(new GithubTeamDetails()
        {
            Description = request.Description!,
            Id = team.Id,
            IsPublic = isPublic,
            Name = request.Name!,
            Slug = team.Slug,
            Maintainers = request.Maintainers,
            Members = request.Members
        });

        static User CreateUserWithLogin(string login)
            => new(default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, login: login, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!);
    }

    private static Team CreateTeam(
            string? url = null,
            string? htmlUrl = null,
            int? id = null,
            string? nodeId = null,
            string? slug = null,
            string? name = null,
            string? description = null,
            TeamPrivacy? privacy = null,
            string? permission = null,
            TeamRepositoryPermissions? permissions = null,
            int? memberCount = null,
            int? repoCount = null,
            Organization? organization = null,
            Team? parent = null,
            string? ldapDistinguishedName = null)
    {
        return new(
            url ?? fixture.Create<string>(),
            htmlUrl ?? fixture.Create<string>(),
            id ?? fixture.Create<int>(),
            nodeId ?? fixture.Create<string>(),
            slug ?? fixture.Create<string>(),
            name ?? fixture.Create<string>(),
            description ?? fixture.Create<string>(),
            privacy ?? fixture.Create<TeamPrivacy>(),
            permission ?? fixture.Create<string>(),
            permissions ?? fixture.Create<TeamRepositoryPermissions>(),
            memberCount ?? fixture.Create<int>(),
            repoCount ?? fixture.Create<int>(),
            organization ?? fixture.Create<Organization>(),
            parent,
            ldapDistinguishedName ?? fixture.Create<string>());
    }
}