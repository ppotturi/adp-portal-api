

namespace ADP.Portal.Core.Git.Entities
{
    public class Group()
    {
        public required string DisplayName { get; set; }

        public GroupType? Type { get; set; }

        public string? Description { get; set; }

        public List<string> GroupMemberships { get; set; } = [];

        public List<string> Members { get; set; } = [];
    }
}
