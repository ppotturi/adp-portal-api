namespace ADP.Portal.Core.Git.Entities
{
    internal record class FluxImagePolicy
    {
        public required string Name { get; internal set; }
        public required string PolicyString { get; internal set; }
    }
}
