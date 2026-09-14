using System;
using System.Text;
using System.Text.Json;
using Xunit;

namespace PayPal.PartnerGateway.Tests;

public class AuthAssertionTests
{
    [Fact]
    public void Create_ProducesThreePartUnsignedJwt_WithEmptySignature()
    {
        var value = AuthAssertion.Create("my-client-id", "MERCHANT123");

        var parts = value.Split('.');
        Assert.Equal(3, parts.Length);
        Assert.Equal(string.Empty, parts[2]);
    }

    [Fact]
    public void Create_EncodesIssuerAndPayerIdInPayload()
    {
        var value = AuthAssertion.Create("my-client-id", "MERCHANT123");
        var payloadJson = DecodeBase64Url(value.Split('.')[1]);

        using var doc = JsonDocument.Parse(payloadJson);
        Assert.Equal("my-client-id", doc.RootElement.GetProperty("iss").GetString());
        Assert.Equal("MERCHANT123", doc.RootElement.GetProperty("payer_id").GetString());
    }

    [Fact]
    public void Create_ThrowsForMissingArguments()
    {
        Assert.Throws<ArgumentException>(() => AuthAssertion.Create("", "MERCHANT123"));
        Assert.Throws<ArgumentException>(() => AuthAssertion.Create("client-id", ""));
    }

    private static string DecodeBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }
}
