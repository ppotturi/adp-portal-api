namespace ADP.Portal.Core.Git.Entities
{
    public class GitRepo
    {
        public GitRepo(string name, string branchName, string organisation)
        {
            Name = name;
            BranchName = branchName;
            Organisation = organisation;
        }

        public string Name { get; set; }
        public string BranchName { get; set; }
        public string Organisation { get; set; }
    }
}
