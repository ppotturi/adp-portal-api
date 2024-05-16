using ADP.Portal.Core.Git.Entities;

namespace ADP.Portal.Core.Git.Services;

public interface IFluxManifestService
{
    public Task<Dictionary<object,object>?> GetFluxServiceTemplatePatchValuesAsync(string templateType);
}
