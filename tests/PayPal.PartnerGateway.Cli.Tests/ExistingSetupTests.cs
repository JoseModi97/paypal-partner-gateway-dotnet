using Xunit;

namespace PayPal.PartnerGateway.Cli.Tests;

public class ExistingSetupTests
{
    [Fact]
    public void ReadAppSettings_ReturnsNull_WhenFileDoesNotExist()
    {
        using var dir = new TempDirectory();

        Assert.Null(ExistingSetup.ReadAppSettings(dir.Path));
    }

    [Fact]
    public void ReadAppSettings_ReturnsNull_WhenPayPalPartnerSectionIsAbsent()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("appsettings.json", "{ \"Logging\": {} }");

        Assert.Null(ExistingSetup.ReadAppSettings(dir.Path));
    }

    [Fact]
    public void ReadAppSettings_ReturnsNull_ForMalformedJson()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("appsettings.json", "{ not valid json");

        Assert.Null(ExistingSetup.ReadAppSettings(dir.Path));
    }

    [Fact]
    public void ReadAppSettings_ParsesAllFields_WhenPresent()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("appsettings.json", """
            {
              "PayPalPartner": {
                "ClientId": "abc",
                "ClientSecret": "secret",
                "Environment": "Live",
                "PartnerAttributionId": "bn-code",
                "WebhookId": "wh-1"
              }
            }
            """);

        var config = ExistingSetup.ReadAppSettings(dir.Path);

        Assert.NotNull(config);
        Assert.Equal("abc", config!.ClientId);
        Assert.Equal("secret", config.ClientSecret);
        Assert.Equal("Live", config.Environment);
        Assert.Equal("bn-code", config.PartnerAttributionId);
        Assert.Equal("wh-1", config.WebhookId);
    }

    [Fact]
    public void ReadAppSettings_LeavesOptionalFieldsNull_WhenAbsent()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("appsettings.json", """
            { "PayPalPartner": { "ClientId": "abc", "ClientSecret": "secret", "Environment": "Sandbox" } }
            """);

        var config = ExistingSetup.ReadAppSettings(dir.Path);

        Assert.NotNull(config);
        Assert.Null(config!.PartnerAttributionId);
        Assert.Null(config.WebhookId);
    }

    [Fact]
    public void FileBasedAppReferencesPackage_ReturnsFalse_WhenFileDoesNotExist()
    {
        Assert.False(ExistingSetup.FileBasedAppReferencesPackage(@"C:\does\not\exist.cs", "PayPal.PartnerGateway"));
    }

    [Fact]
    public void FileBasedAppReferencesPackage_DetectsVersionedDirective()
    {
        using var dir = new TempDirectory();
        var path = dir.WriteFile("app.cs", "#:package PayPal.PartnerGateway@1.0.0\nConsole.WriteLine(1);");

        Assert.True(ExistingSetup.FileBasedAppReferencesPackage(path, "PayPal.PartnerGateway"));
        Assert.False(ExistingSetup.FileBasedAppReferencesPackage(path, "PayPal.PartnerGateway.AspNetCore"));
    }

    [Fact]
    public void FileBasedAppReferencesPackage_DetectsUnversionedDirective()
    {
        using var dir = new TempDirectory();
        var path = dir.WriteFile("app.cs", "#:package PayPal.PartnerGateway\nConsole.WriteLine(1);");

        Assert.True(ExistingSetup.FileBasedAppReferencesPackage(path, "PayPal.PartnerGateway"));
    }

    [Fact]
    public void FileBasedAppReferencesPackage_ReturnsFalse_WhenNoDirectivePresent()
    {
        using var dir = new TempDirectory();
        var path = dir.WriteFile("app.cs", "Console.WriteLine(1);");

        Assert.False(ExistingSetup.FileBasedAppReferencesPackage(path, "PayPal.PartnerGateway"));
    }
}
