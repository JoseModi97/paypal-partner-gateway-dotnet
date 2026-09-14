using System.Text.Json.Serialization;

namespace PayPal.PartnerGateway.Models;

/// <summary>
/// One RFC 6902 JSON Patch operation, used by every PATCH endpoint in the PayPal Partner APIs
/// (webhooks, managed accounts, and others).
/// </summary>
public class JsonPatchOperation
{
    /// <summary>"add", "replace", or "remove".</summary>
    [JsonPropertyName("op")]
    public string Op { get; set; } = "replace";

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public object? Value { get; set; }

    public JsonPatchOperation()
    {
    }

    public JsonPatchOperation(string op, string path, object? value = null)
    {
        Op = op;
        Path = path;
        Value = value;
    }
}
