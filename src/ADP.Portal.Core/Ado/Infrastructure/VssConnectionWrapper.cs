
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace ADP.Portal.Core.Ado.Infrastructure
{
    public interface IVssConnection : IDisposable
    {
        public T GetClient<T>() where T : VssHttpClientBase => this.GetClientAsync<T>().SyncResult<T>();
        public Task<T> GetClientAsync<T>(CancellationToken cancellationToken = default(CancellationToken)) where T : VssHttpClientBase;
    }
    public class VssConnectionWrapper : VssConnection, IVssConnection
    {
        public VssConnectionWrapper(Uri baseUrl, VssCredentials credentials) : base(baseUrl, credentials)
        {
        }
    }
}
