using System.ComponentModel.DataAnnotations;

namespace ADP.Portal.Api.Models.Group;

public sealed class SetGroupMembersRequest
{
    [Required]
    public required IEnumerable<string> TechUserMembers { get; set; }

    [Required]
    public required IEnumerable<string> NonTechUserMembers { get; set; }

    [Required]
    public required IEnumerable<string> AdminMembers { get; set; }
}
