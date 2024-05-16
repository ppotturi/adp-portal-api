using ADP.Portal.Core.Git.Entities;

namespace ADP.Portal.Core.Git.Services;

public interface IFluxTemplateService
{
    Task<IEnumerable<KeyValuePair<string, FluxTemplateFile>>> GetFluxTemplatesAsync();
    Task<FluxTemplateFile?> GetFluxTemplateAsync(string path);
}
