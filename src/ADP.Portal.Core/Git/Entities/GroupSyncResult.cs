namespace ADP.Portal.Core.Git.Entities
{
    public class GroupSyncResult
    {
        public GroupSyncResult()
        {
            Error = [];
        }
        public List<string> Error { get; set; }
    }
}
