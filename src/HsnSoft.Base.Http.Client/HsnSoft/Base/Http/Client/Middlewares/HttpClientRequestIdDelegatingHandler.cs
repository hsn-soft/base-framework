using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.Http.Client.Middlewares;

public class HttpClientRequestIdDelegatingHandler : DelegatingHandler
{
    public HttpClientRequestIdDelegatingHandler()
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put)
        {
            if (!request.Headers.Contains("x-requestid"))
            {
                request.Headers.Add("x-requestid", Guid.CreateVersion7().ToString());
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}