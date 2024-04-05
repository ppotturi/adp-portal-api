using YamlDotNet.Serialization;

namespace ADP.Portal.Core.Git.Entities
{
    public class Group()
    {
        public required string DisplayName { get; set; }

        public GroupType Type { get; set; }

        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? Description { get; set; }

        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitDefaults)]
        public List<string> GroupMemberships { get; set; } = [];

        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitDefaults)]
        public List<string> Members { get; set; } = [];
    }
}
