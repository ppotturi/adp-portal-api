namespace ADP.Portal.Api.Models.Group
{
    public sealed class CreateGroupsConfigRequest
    {
        public required List<string> Members { get; set; }
    }
}
