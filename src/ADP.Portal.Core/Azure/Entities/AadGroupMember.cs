namespace ADP.Portal.Core.Azure.Entities
{
    public class AadGroupMember
    {
        public AadGroupMember(string id, string? userPrincipalName, string? displayName)
        {
            Id = id;
            UserPrincipalName = userPrincipalName;
            DisplayName = displayName;
        }
        public string Id { get; set; }
        public string? UserPrincipalName { get; set; }
        public string? DisplayName { get; set; }
    }
}
