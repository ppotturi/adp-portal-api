namespace ADP.Portal.Core.Git.Entities
{
    public sealed record FluxTemplateFile
    {
        public Dictionary<object, object> Content { get; init; }
        public FluxTemplateFile(Dictionary<object, object> content)
        {
            Content = content;
        }
    }
}
