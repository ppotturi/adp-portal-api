using ADP.Portal.Core.Git.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using System.Diagnostics.CodeAnalysis;

namespace ADP.Portal.Core.Git.Services;

public class GitHubService : IGitHubService
{
    private readonly IGitHubClient client;
    private readonly IOptions<GitHubOptions> options;
    private readonly ILogger<GitHubService> logger;

    public GitHubService(IGitHubClient client, IOptions<GitHubOptions> options, ILogger<GitHubService> logger)
    {
        this.client = client;
        this.options = options;
        this.logger = logger;
    }

    public async Task<GithubTeamDetails> SyncTeamAsync(GithubTeamUpdate team, CancellationToken cancellationToken)
    {
        logger.LogInformation("Setting team details for team {TeamName}", team.Name);
        var currentTeam = await GetTeamDetails(team.Name);

        if (currentTeam is null)
        {
            logger.LogInformation("Team {TeamName} does not exist, it will be created.", team.Name);
            return await CreateTeamAsync(team);
        }
        else
        {
            logger.LogInformation("Team {TeamName} already exists ({TeamId}), it will be updated.", team.Name, currentTeam.Id);
            return await UpdateTeamAsync(currentTeam, team);
        }
    }

    private async Task<GithubTeamDetails> UpdateTeamAsync(GithubTeamDetails currentTeam, GithubTeamUpdate team)
    {
        if (TryBuildUpdate(currentTeam, team, out var update))
        {
            logger.LogInformation("Updating details for team {TeamName} ({TeamId}).", currentTeam.Name, currentTeam.Id);
            var updatedTeam = await client.Organization.Team.Update(currentTeam.Id, update);
            logger.LogInformation("Team {TeamName} ({TeamId}) has been updated.", team.Name, currentTeam.Id);

            currentTeam = currentTeam with
            {
                Description = updatedTeam.Description,
                IsPublic = updatedTeam.Privacy.Value is TeamPrivacy.Closed,
                Id = updatedTeam.Id,
                Name = updatedTeam.Name,
                Slug = updatedTeam.Slug
            };
        }

        await SyncTeamMembers(
            currentTeam.Id,
            BuildTeamRoleDictionary(currentTeam.Maintainers, currentTeam.Members),
            BuildTeamRoleDictionary(team.Maintainers, team.Members));

        return currentTeam with
        {
            Maintainers = team.Maintainers ?? currentTeam.Maintainers,
            Members = team.Members ?? currentTeam.Members
        };
    }

    private static bool TryBuildUpdate(GithubTeamDetails currentTeam, GithubTeamUpdate team, [NotNullWhen(true)] out UpdateTeam? update)
    {
        if (IsUnchanged(currentTeam.Description, team.Description)
            && IsUnchanged(currentTeam.IsPublic, team.IsPublic)
            && IsUnchanged(currentTeam.Name, team.Name))
        {
            update = null;
            return false;
        }

        update = new(team.Name)
        {
            Description = team.Description ?? currentTeam.Description,
            Privacy = (team.IsPublic ?? currentTeam.IsPublic) ? TeamPrivacy.Closed : TeamPrivacy.Secret
        };
        return true;
    }

    private static bool IsUnchanged<T>(T current, T? update)
    {
        return update is null || Equals(current, update);
    }

    private static Dictionary<string, TeamRole> BuildTeamRoleDictionary(IEnumerable<string>? maintainers, IEnumerable<string>? members)
    {
        maintainers ??= [];
        members ??= [];

        members = members.Except(maintainers);

        return Enumerable.Concat(
            maintainers.Select(m => KeyValuePair.Create(m, TeamRole.Maintainer)),
            members.Select(m => KeyValuePair.Create(m, TeamRole.Member)))
            .ToDictionary();
    }

    private async Task<GithubTeamDetails> CreateTeamAsync(GithubTeamUpdate team)
    {
        var request = new NewTeam(team.Name)
        {
            Description = team.Description,
            Privacy = (team.IsPublic ?? true) ? TeamPrivacy.Closed : TeamPrivacy.Secret,
        };

        foreach (var member in team.Maintainers ?? [])
            request.Maintainers.Add(member);

        logger.LogInformation("Creating team {TeamName}.", team.Name);
        var newTeam = await client.Organization.Team.Create(options.Value.Organisation, request);

        logger.LogInformation("Team {TeamName} ({TeamId}) has been created, syncing members.", newTeam.Name, newTeam.Id);
        await SyncTeamMembers(newTeam.Id, [], BuildTeamRoleDictionary(null, team.Members));

        return new()
        {
            Id = newTeam.Id,
            Name = newTeam.Name,
            Slug = newTeam.Slug,
            Description = newTeam.Description,
            IsPublic = newTeam.Privacy.Value == TeamPrivacy.Closed,
            Maintainers = request.Maintainers,
            Members = team.Members ?? []
        };
    }

    private async Task SyncTeamMembers(int teamId, Dictionary<string, TeamRole> currentMembers, Dictionary<string, TeamRole> targetMembers)
    {
        var setMembers = targetMembers
            .Where(kvp => !currentMembers.TryGetValue(kvp.Key, out var currentRole) || currentRole < kvp.Value)
            .Select(m => SetMemberRole(m.Key, m.Value));
        var removeMembers = currentMembers.Keys.Except(targetMembers.Keys)
            .Select(RemoveMember);
        var mutations = setMembers.Concat(removeMembers);

        // If we want to run the changes in parallel, change to `await Task.WhenAll(mutations)`
        foreach (var task in mutations)
            await task;

        async Task SetMemberRole(string member, TeamRole role)
        {
            logger.LogInformation("Setting {Member} membership of {TeamId} to {Role}", member, teamId, role);
            try
            {
                await client.Organization.Team.AddOrEditMembership(teamId, member, new(role));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while setting {Member} membership of {TeamId} to {Role}.", member, teamId, role);
            }
        }

        async Task RemoveMember(string member)
        {
            logger.LogInformation("Removing {Member} from {TeamId}.", member, teamId);
            try
            {
                await client.Organization.Team.RemoveMembership(teamId, member);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while removing {Member} from {TeamId}.", member, teamId);
            }
        }
    }

    private async Task<GithubTeamDetails?> GetTeamDetails(string teamName)
    {
        logger.LogInformation("Getting details of {TeamName}.", teamName);
        var team = await GetTeamByNameOrDefault(teamName);
        if (team is null)
        {
            logger.LogInformation("Cannot find team {TeamName}.", teamName);
            return null;
        }

        logger.LogInformation("Getting members and maintainers of {TeamName}.", teamName);
        var members = await client.Organization.Team.GetAllMembers(team.Id, new TeamMembersRequest(TeamRoleFilter.Member));
        var maintainers = await client.Organization.Team.GetAllMembers(team.Id, new TeamMembersRequest(TeamRoleFilter.Maintainer));

        logger.LogInformation("Retreived all needed data about team {TeamName}.", teamName);
        return new()
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            IsPublic = team.Privacy.Value is not TeamPrivacy.Secret,
            Maintainers = maintainers.Select(u => u.Login).ToArray(),
            Members = members.Select(u => u.Login).ToArray(),
            Slug = team.Slug
        };
    }

    private async Task<Team?> GetTeamByNameOrDefault(string teamName)
    {
        try
        {
            return await client.Organization.Team.GetByName(options.Value.Organisation, teamName);
        }
        catch (NotFoundException)
        {
            return default;
        }
    }
}