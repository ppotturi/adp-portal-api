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
            Organisation = Guid.NewGuid().ToString(),
            AdminLogin = "adp-platform",
            BlacklistedTeams =
            {
                "ADP-Platform-Admins"
            }
        };
        client = Substitute.For<IGitHubClient>();
        sut = new GitHubService(client, Options.Create(options), logger);
    }

    [Test]
    [TestCase(true, TeamPrivacy.Closed)]
    [TestCase(false, TeamPrivacy.Secret)]
    public async Task SyncTeamAsync_CreatesATeamThatDoesntAlreadyExist_WhenTheIdIsNull(bool isPublic, TeamPrivacy privacy)
    {
        // arrange
        using var cts = new CancellationTokenSource();
        var request = fixture.Create<GithubTeamUpdate>();
        request.IsPublic = isPublic;
        request.Maintainers = fixture.CreateMany<string>(3).ToArray();
        request.Members = fixture.CreateMany<string>(3).ToArray();
        request.Id = null;

        var org = CreateOrganization(login: options.Organisation);
        var team = CreateTeam(privacy: privacy, organization: org);

        client.Organization.Team.Create(default, default)
            .ReturnsForAnyArgs(team);

        // act
        var actual = await sut.SyncTeamAsync(request, cts.Token);

        // assert
        _ = client.Organization.Team.Received(0).Get(Arg.Any<int>());
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
    public async Task SyncTeamAsync_CreatesATeamWhenTheTeamExistsButIsInAnotherOrg(bool isPublic, TeamPrivacy privacy)
    {
        // arrange
        using var cts = new CancellationTokenSource();
        var request = fixture.Create<GithubTeamUpdate>();
        request.IsPublic = isPublic;
        request.Maintainers = fixture.CreateMany<string>(3).ToArray();
        request.Members = fixture.CreateMany<string>(3).ToArray();
        request.Id = 123;

        var org = CreateOrganization();
        var team = CreateTeam(privacy: privacy, organization: org);

        client.Organization.Team.Get(123)
            .Returns(team);

        client.Organization.Team.Create(default, default)
            .ReturnsForAnyArgs(team);

        // act
        var actual = await sut.SyncTeamAsync(request, cts.Token);

        // assert
        _ = client.Organization.Team.Received(1).Get(123);
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
    public async Task SyncTeamAsync_CreatesATeamThatDoesntAlreadyExist(bool isPublic, TeamPrivacy privacy)
    {
        // arrange
        using var cts = new CancellationTokenSource();
        var request = fixture.Create<GithubTeamUpdate>();
        request.IsPublic = isPublic;
        request.Maintainers = fixture.CreateMany<string>(3).ToArray();
        request.Members = fixture.CreateMany<string>(3).ToArray();

        var org = CreateOrganization(login: options.Organisation);
        var team = CreateTeam(privacy: privacy, organization: org);

        client.Organization.Team.Get(request.Id!.Value)
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
    public async Task SyncTeamAsync_ReturnsNullWhenCreateFails(bool isPublic, TeamPrivacy privacy)
    {
        // arrange
        using var cts = new CancellationTokenSource();
        var request = fixture.Create<GithubTeamUpdate>();
        request.IsPublic = isPublic;
        request.Maintainers = fixture.CreateMany<string>(3).ToArray();
        request.Members = fixture.CreateMany<string>(3).ToArray();

        var org = CreateOrganization(login: options.Organisation);
        var team = CreateTeam(privacy: privacy, organization: org);

        client.Organization.Team.Get(request.Id!.Value)
            .ThrowsAsync(new NotFoundException("TESTING", HttpStatusCode.NotFound));
        client.Organization.Team.GetByName(options.Organisation, request.Name)
            .ThrowsAsync(new NotFoundException("TESTING", HttpStatusCode.NotFound));

        client.Organization.Team.Create(default, default)
            .ThrowsAsyncForAnyArgs(new ApiValidationException());

        // act
        var actual = await sut.SyncTeamAsync(request, cts.Token);

        // assert
        _ = client.Organization.Team.Received(1).Create(options.Organisation, Arg.Is<NewTeam>(t =>
            t.Name == request.Name
            && t.Description == request.Description
            && t.Privacy == privacy
            && t.Maintainers.SequenceEqual(request.Maintainers)));
        _ = client.Organization.Team.Received(0).Update(Arg.Any<int>(), Arg.Any<UpdateTeam>());
        _ = client.Organization.Team.Received(1).GetByName(options.Organisation, request.Name);

        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Members.ElementAt(0), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));
        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Members.ElementAt(1), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));
        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Members.ElementAt(2), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));

        actual.Should().BeNull();
    }

    [Test]
    [TestCase(true, TeamPrivacy.Closed)]
    [TestCase(false, TeamPrivacy.Secret)]
    public async Task SyncTeamAsync_ReturnsNullWhenCreateFails_AndExistingTeamIsBlacklisted(bool isPublic, TeamPrivacy privacy)
    {
        // arrange
        using var cts = new CancellationTokenSource();
        var request = fixture.Create<GithubTeamUpdate>();
        request.Name = "adp-platform-admins";
        request.IsPublic = isPublic;
        request.Maintainers = fixture.CreateMany<string>(3).ToArray();
        request.Members = fixture.CreateMany<string>(3).ToArray();

        var org = CreateOrganization(login: options.Organisation);
        var team = CreateTeam(privacy: privacy, organization: org);

        client.Organization.Team.Get(request.Id!.Value)
            .ThrowsAsync(new NotFoundException("TESTING", HttpStatusCode.NotFound));

        client.Organization.Team.Create(default, default)
            .ThrowsAsyncForAnyArgs(new ApiValidationException());

        // act
        var actual = await sut.SyncTeamAsync(request, cts.Token);

        // assert
        _ = client.Organization.Team.Received(1).Create(options.Organisation, Arg.Is<NewTeam>(t =>
            t.Name == request.Name
            && t.Description == request.Description
            && t.Privacy == privacy
            && t.Maintainers.SequenceEqual(request.Maintainers)));
        _ = client.Organization.Team.Received(0).Update(Arg.Any<int>(), Arg.Any<UpdateTeam>());
        _ = client.Organization.Team.Received(0).GetByName(options.Organisation, request.Name);

        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Members.ElementAt(0), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));
        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Members.ElementAt(1), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));
        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Members.ElementAt(2), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));

        actual.Should().BeNull();
    }

    [Test]
    [TestCase(true, TeamPrivacy.Closed)]
    [TestCase(false, TeamPrivacy.Secret)]
    public async Task SyncTeamAsync_ReturnsNullWhenCreateFails_AndExistingTeamDoesntHaveAdminMember(bool isPublic, TeamPrivacy privacy)
    {
        // arrange
        using var cts = new CancellationTokenSource();
        var request = fixture.Create<GithubTeamUpdate>();
        var currentMaintainers = fixture.CreateMany<string>(3).ToArray();
        var currentMembers = fixture.CreateMany<string>(3).ToArray();
        request.IsPublic = isPublic;
        request.Maintainers = fixture.CreateMany<string>(3).ToArray();
        request.Members = fixture.CreateMany<string>(3).ToArray();

        var org = CreateOrganization(login: options.Organisation);
        var team = CreateTeam(privacy: privacy, organization: org);

        client.Organization.Team.Get(request.Id!.Value)
            .ThrowsAsync(new NotFoundException("TESTING", HttpStatusCode.NotFound));
        client.Organization.Team.GetByName(options.Organisation, request.Name)
            .Returns(team);
        client.Organization.Team.GetAllMembers(team.Id, Arg.Is<TeamMembersRequest>(t =>
            t.Role == TeamRoleFilter.Member))
            .Returns(currentMembers.Select(x => CreateUser(login: x)).ToList());
        client.Organization.Team.GetAllMembers(team.Id, Arg.Is<TeamMembersRequest>(t =>
            t.Role == TeamRoleFilter.Maintainer))
            .Returns(currentMaintainers.Select(x => CreateUser(login: x)).ToList());

        client.Organization.Team.Create(default, default)
            .ThrowsAsyncForAnyArgs(new ApiValidationException());

        // act
        var actual = await sut.SyncTeamAsync(request, cts.Token);

        // assert
        _ = client.Organization.Team.Received(1).Create(options.Organisation, Arg.Is<NewTeam>(t =>
            t.Name == request.Name
            && t.Description == request.Description
            && t.Privacy == privacy
            && t.Maintainers.SequenceEqual(request.Maintainers)));
        _ = client.Organization.Team.Received(0).Update(Arg.Any<int>(), Arg.Any<UpdateTeam>());
        _ = client.Organization.Team.Received(1).GetByName(options.Organisation, request.Name);

        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Members.ElementAt(0), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));
        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Members.ElementAt(1), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));
        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Members.ElementAt(2), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));

        actual.Should().BeNull();
    }

    [Test]
    [TestCase(true, TeamPrivacy.Closed)]
    [TestCase(false, TeamPrivacy.Secret)]
    public async Task SyncTeamAsync_AdoptsATeamThatAlreadyExistsWithTheCorrectMember(bool isPublic, TeamPrivacy privacy)
    {
        // arrange
        using var cts = new CancellationTokenSource();
        var request = fixture.Create<GithubTeamUpdate>();
        var currentMaintainers = fixture.CreateMany<string>(3).ToArray();
        var currentMembers = fixture.CreateMany<string>(3).ToArray();
        currentMembers = [.. currentMembers, "adp-platform"];
        request.IsPublic = isPublic;

        var org = CreateOrganization(login: options.Organisation);
        var team = CreateTeam(privacy: privacy, organization: org);

        client.Organization.Team.Get(request.Id!.Value)
            .ThrowsAsync(new NotFoundException("TESTING", HttpStatusCode.NotFound));
        client.Organization.Team.GetByName(options.Organisation, request.Name)
            .Returns(team);
        client.Organization.Team.GetAllMembers(team.Id, Arg.Is<TeamMembersRequest>(t =>
            t.Role == TeamRoleFilter.Member))
            .Returns(currentMembers.Select(x => CreateUser(login: x)).ToList());
        client.Organization.Team.GetAllMembers(team.Id, Arg.Is<TeamMembersRequest>(t =>
            t.Role == TeamRoleFilter.Maintainer))
            .Returns(currentMaintainers.Select(x => CreateUser(login: x)).ToList());

        client.Organization.Team.Create(default, default)
            .ThrowsAsyncForAnyArgs(new ApiValidationException());
        client.Organization.Team.Update(team.Id, Arg.Is<UpdateTeam>(t =>
            t.Name == request.Name
            && t.Description == request.Description
            && t.Privacy == privacy))
            .ThrowsAsync(new ApiValidationException());

        // act
        var actual = await sut.SyncTeamAsync(request, cts.Token);

        // assert
        _ = client.Organization.Team.Received(1).Create(options.Organisation, Arg.Is<NewTeam>(t =>
            t.Name == request.Name
            && t.Description == request.Description
            && t.Privacy == privacy
            && t.Maintainers.SequenceEqual(request.Maintainers ?? Enumerable.Empty<string>())));
        _ = client.Organization.Team.Received(1).Update(team.Id, Arg.Is<UpdateTeam>(t =>
            t.Name == request.Name
            && t.Description == request.Description
            && t.Privacy == privacy));
        _ = client.Organization.Team.Received(1).GetByName(options.Organisation, request.Name);

        actual.Should().BeNull();
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

        var org = CreateOrganization(login: options.Organisation);
        var team = CreateTeam(privacy: privacy, organization: org);

        client.Organization.Team.Get(request.Id!.Value)
            .Returns(team);
        client.Organization.Team.GetAllMembers(team.Id, Arg.Is<TeamMembersRequest>(t =>
            t.Role == TeamRoleFilter.Member))
            .Returns(currentMembers.Select(x => CreateUser(login: x)).ToList());
        client.Organization.Team.GetAllMembers(team.Id, Arg.Is<TeamMembersRequest>(t =>
            t.Role == TeamRoleFilter.Maintainer))
            .Returns(currentMaintainers.Select(x => CreateUser(login: x)).ToList());
        client.Organization.Team.Update(team.Id, Arg.Is<UpdateTeam>(t =>
            t.Name == request.Name
            && t.Description == request.Description
            && t.Privacy == privacy))
            .ThrowsAsync(new ApiValidationException());

        // act
        var actual = await sut.SyncTeamAsync(request, cts.Token);

        // assert
        _ = client.Organization.Team.Received(1).Update(team.Id, Arg.Is<UpdateTeam>(t =>
            t.Name == request.Name
            && t.Description == request.Description
            && t.Privacy == privacy));

        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Members.ElementAt(0), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));
        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Members.ElementAt(1), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));
        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Members.ElementAt(2), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Member));
        _ = client.Organization.Team.Received(0).RemoveMembership(team.Id, currentMembers[0]);
        _ = client.Organization.Team.Received(0).RemoveMembership(team.Id, currentMembers[1]);
        _ = client.Organization.Team.Received(0).RemoveMembership(team.Id, currentMembers[2]);
        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Maintainers.ElementAt(0), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Maintainer));
        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Maintainers.ElementAt(1), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Maintainer));
        _ = client.Organization.Team.Received(0).AddOrEditMembership(team.Id, request.Maintainers.ElementAt(2), Arg.Is<UpdateTeamMembership>(m => m.Role == TeamRole.Maintainer));
        _ = client.Organization.Team.Received(0).RemoveMembership(team.Id, currentMaintainers[0]);
        _ = client.Organization.Team.Received(0).RemoveMembership(team.Id, currentMaintainers[1]);
        _ = client.Organization.Team.Received(0).RemoveMembership(team.Id, currentMaintainers[2]);

        actual.Should().BeNull();
    }

    [Test]
    [TestCase(true, TeamPrivacy.Closed)]
    [TestCase(false, TeamPrivacy.Secret)]
    public async Task SyncTeamAsync_ReturnsNullWhenUpdateFails(bool isPublic, TeamPrivacy privacy)
    {
        // arrange
        using var cts = new CancellationTokenSource();
        var request = fixture.Create<GithubTeamUpdate>();
        var currentMaintainers = fixture.CreateMany<string>(3).ToArray();
        var currentMembers = fixture.CreateMany<string>(3).ToArray();
        request.IsPublic = isPublic;
        request.Maintainers = fixture.CreateMany<string>(2).Concat(currentMaintainers.Take(1)).ToArray();
        request.Members = fixture.CreateMany<string>(2).Concat(currentMembers.Take(1)).ToArray();

        var org = CreateOrganization(login: options.Organisation);
        var team = CreateTeam(privacy: privacy, organization: org);

        client.Organization.Team.Get(request.Id!.Value)
            .Returns(team);
        client.Organization.Team.GetAllMembers(team.Id, Arg.Is<TeamMembersRequest>(t =>
            t.Role == TeamRoleFilter.Member))
            .Returns(currentMembers.Select(x => CreateUser(login: x)).ToList());
        client.Organization.Team.GetAllMembers(team.Id, Arg.Is<TeamMembersRequest>(t =>
            t.Role == TeamRoleFilter.Maintainer))
            .Returns(currentMaintainers.Select(x => CreateUser(login: x)).ToList());
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

        var org = CreateOrganization(login: options.Organisation);
        var team = CreateTeam(
            description: request.Description,
            privacy: privacy,
            name: request.Name,
            organization: org);

        client.Organization.Team.Get(request.Id!.Value)
            .Returns(team);
        client.Organization.Team.GetAllMembers(team.Id, Arg.Is<TeamMembersRequest>(t =>
            t.Role == TeamRoleFilter.Member))
            .Returns(request.Members.Select(x => CreateUser(login: x)).ToList());
        client.Organization.Team.GetAllMembers(team.Id, Arg.Is<TeamMembersRequest>(t =>
            t.Role == TeamRoleFilter.Maintainer))
            .Returns(request.Maintainers.Select(x => CreateUser(login: x)).ToList());

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
    }

    private static User CreateUser(
        string? avatarUrl = default,
        string? bio = default,
        string? blog = default,
        int? collaborators = default,
        string? company = default,
        DateTimeOffset? createdAt = default,
        DateTimeOffset? updatedAt = default,
        int? diskUsage = default,
        string? email = default,
        int? followers = default,
        int? following = default,
        bool? hireable = default,
        string? htmlUrl = default,
        int? totalPrivateRepos = default,
        int? id = default,
        string? location = default,
        string? login = default,
        string? name = default,
        string? nodeId = default,
        int? ownedPrivateRepos = default,
        Plan? plan = default,
        int? privateGists = default,
        int? publicGists = default,
        int? publicRepos = default,
        string? url = default,
        RepositoryPermissions? permissions = default,
        bool? siteAdmin = default,
        string? ldapDistinguishedName = default,
        DateTimeOffset? suspendedAt = default)
    {
        return new User(
            avatarUrl ?? fixture.Create<string>(),
            bio ?? fixture.Create<string>(),
            blog ?? fixture.Create<string>(),
            collaborators ?? fixture.Create<int>(),
            company ?? fixture.Create<string>(),
            createdAt ?? fixture.Create<DateTimeOffset>(),
            updatedAt ?? fixture.Create<DateTimeOffset>(),
            diskUsage ?? fixture.Create<int>(),
            email ?? fixture.Create<string>(),
            followers ?? fixture.Create<int>(),
            following ?? fixture.Create<int>(),
            hireable ?? fixture.Create<bool>(),
            htmlUrl ?? fixture.Create<string>(),
            totalPrivateRepos ?? fixture.Create<int>(),
            id ?? fixture.Create<int>(),
            location ?? fixture.Create<string>(),
            login ?? fixture.Create<string>(),
            name ?? fixture.Create<string>(),
            nodeId ?? fixture.Create<string>(),
            ownedPrivateRepos ?? fixture.Create<int>(),
            plan ?? fixture.Create<Plan>(),
            privateGists ?? fixture.Create<int>(),
            publicGists ?? fixture.Create<int>(),
            publicRepos ?? fixture.Create<int>(),
            url ?? fixture.Create<string>(),
            permissions ?? fixture.Create<RepositoryPermissions>(),
            siteAdmin ?? fixture.Create<bool>(),
            ldapDistinguishedName ?? fixture.Create<string>(),
            suspendedAt ?? fixture.Create<DateTimeOffset>());
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

    private static Organization CreateOrganization(
            string? avatarUrl = null,
            string? bio = null,
            string? blog = null,
            int? collaborators = null,
            string? company = null,
            DateTimeOffset? createdAt = null,
            int? diskUsage = null,
            string? email = null,
            int? followers = null,
            int? following = null,
            bool? hireable = null,
            string? htmlUrl = null,
            int? totalPrivateRepos = null,
            int? id = null,
            string? nodeId = null,
            string? location = null,
            string? login = null,
            string? name = null,
            int? ownedPrivateRepos = null,
            Plan? plan = null,
            int? privateGists = null,
            int? publicGists = null,
            int? publicRepos = null,
            string? url = null,
            string? billingAddress = null,
            string? reposUrl = null,
            string? eventsUrl = null,
            string? hooksUrl = null,
            string? issuesUrl = null,
            string? membersUrl = null,
            string? publicMembersUrl = null,
            string? description = null,
            bool? isVerified = null,
            bool? hasOrganizationProjects = null,
            bool? hasRepositoryProjects = null,
            DateTimeOffset? updatedAt = null)
    {
        return new Organization(
            avatarUrl ?? fixture.Create<string>(),
            bio ?? fixture.Create<string>(),
            blog ?? fixture.Create<string>(),
            collaborators ?? fixture.Create<int>(),
            company ?? fixture.Create<string>(),
            createdAt ?? fixture.Create<DateTimeOffset>(),
            diskUsage ?? fixture.Create<int>(),
            email ?? fixture.Create<string>(),
            followers ?? fixture.Create<int>(),
            following ?? fixture.Create<int>(),
            hireable ?? fixture.Create<bool?>(),
            htmlUrl ?? fixture.Create<string>(),
            totalPrivateRepos ?? fixture.Create<int>(),
            id ?? fixture.Create<int>(),
            nodeId ?? fixture.Create<string>(),
            location ?? fixture.Create<string>(),
            login ?? fixture.Create<string>(),
            name ?? fixture.Create<string>(),
            ownedPrivateRepos ?? fixture.Create<int>(),
            plan ?? fixture.Create<Plan>(),
            privateGists ?? fixture.Create<int>(),
            publicGists ?? fixture.Create<int>(),
            publicRepos ?? fixture.Create<int>(),
            url ?? fixture.Create<string>(),
            billingAddress ?? fixture.Create<string>(),
            reposUrl ?? fixture.Create<string>(),
            eventsUrl ?? fixture.Create<string>(),
            hooksUrl ?? fixture.Create<string>(),
            issuesUrl ?? fixture.Create<string>(),
            membersUrl ?? fixture.Create<string>(),
            publicMembersUrl ?? fixture.Create<string>(),
            description ?? fixture.Create<string>(),
            isVerified ?? fixture.Create<bool>(),
            hasOrganizationProjects ?? fixture.Create<bool>(),
            hasRepositoryProjects ?? fixture.Create<bool>(),
            updatedAt ?? fixture.Create<DateTimeOffset>());
    }
}