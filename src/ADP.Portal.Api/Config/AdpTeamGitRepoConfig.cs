namespace ADP.Portal.Api.Config
{
    public class AdpTeamGitRepoConfig
    {
        public required string RepoName { get; set; }
        public required string BranchName { get; set; }
        public required string Organisation { get; set;}
        public required GitHubAppAuthConfig Auth { get; set; }
        public class GitHubAppAuthConfig
        {
            public required string AppName { get; set; }
            public required int AppId { get; set; }
            public required string PrivateKeyBase64 { get; set; }
        }
    }
}
