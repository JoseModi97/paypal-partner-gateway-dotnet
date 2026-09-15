using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PayPal.PartnerGateway.Tests;

/// <summary>Returns a scripted sequence of responses, recording every request it sees.</summary>
internal class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode StatusCode, string Body)> _responses;
    public List<HttpRequestMessage> Requests { get; } = new();

    /// <summary>
    /// The request body read *before* the caller's `using` block disposes the request/content -
    /// index-aligned with <see cref="Requests"/>. Null for a request with no content.
    /// </summary>
    public List<string?> RequestBodies { get; } = new();

    /// <summary>The Content-Type media type of each request, snapshotted for the same reason as <see cref="RequestBodies"/>.</summary>
    public List<string?> RequestContentTypes { get; } = new();

    public FakeHttpMessageHandler(params (HttpStatusCode StatusCode, string Body)[] responses)
    {
        _responses = new Queue<(HttpStatusCode, string)>(responses);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        RequestContentTypes.Add(request.Content?.Headers.ContentType?.MediaType);

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("No more scripted responses configured.");
        }

        var (statusCode, body) = _responses.Dequeue();
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body)
        };
        return response;
    }
}
