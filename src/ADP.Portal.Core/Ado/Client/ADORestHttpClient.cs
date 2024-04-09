using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace ADP.Portal.Core.Ado.Client
{
    public class AdoRestHttpClient : VssHttpClientBase
    {


        public AdoRestHttpClient(Uri baseUrl, VssCredentials credentials) : base(baseUrl, credentials)
        {

        }

        public AdoRestHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings) : base(baseUrl, credentials, settings)
        {

        }

        public AdoRestHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers) : base(baseUrl, credentials, handlers)

        {

        }

        public AdoRestHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler) : base(baseUrl, pipeline, disposeHandler)

        {

        }

        public AdoRestHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers) : base(baseUrl, credentials, settings, handlers)

        {

        }

        public virtual HttpClient GetHttpClient()
        {
            return base.Client;
        }
    }
}
