namespace ADP.Portal.Core.Git.Entities
{
    public class Group()
    {
        public required string DisplayName { get; set; }

        public bool ManageMembersOnly { get; set; } = default;

        public string? Description { get; set; }

        public List<string> GroupMemberships { get; set; } = [];

        public List<string> Members { get; set; } = [];
    }
}
