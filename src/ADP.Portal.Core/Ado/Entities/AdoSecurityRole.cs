namespace ADP.Portal.Api.Models.Ado
{
    public class AdoSecurityRole
    {
        public required string roleName { get; set; }
        public required string userId { get; set; }
    }

    public class AdoSecurityRoleObj
    {
        public required AdoIdentity identity { get; set; }
        public required AdoRole role { get; set; }
    }

    public class AdoIdentity
    {
        public required string displayName { get; set; }
        public required string id { get; set; }
    }

    public class AdoRole
    {
        public required string name { get; set; }
    }

    public class JsonAdoSecurityRoleWrapper
    {
        public int count { get; set; }

        public List<AdoSecurityRoleObj>? value { get; set; }

    }
}
