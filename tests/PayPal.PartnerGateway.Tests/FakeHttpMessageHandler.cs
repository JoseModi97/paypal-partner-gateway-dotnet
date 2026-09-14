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

    public FakeHttpMessageHandler(params (HttpStatusCode StatusCode, string Body)[] responses)
    {
        _responses = new Queue<(HttpStatusCode, string)>(responses);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("No more scripted responses configured.");
        }

        var (statusCode, body) = _responses.Dequeue();
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body)
        };
        return Task.FromResult(response);
    }
}
