namespace ADP.Portal.Core.Azure.Entities
{
    public class AadGroupMember
    {
        public AadGroupMember(string id, string userPrincipalName)
        {
            Id = id;
            UserPrincipalName = userPrincipalName;
        }
        public string Id { get; set; }
        public string UserPrincipalName { get; set; }
    }
}
