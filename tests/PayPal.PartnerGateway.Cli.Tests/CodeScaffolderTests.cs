using System.IO;
using Xunit;

namespace PayPal.PartnerGateway.Cli.Tests;

public class CodeScaffolderTests
{
    [Theory]
    [InlineData(ProjectFrameworkKind.AspNetCoreWeb, "Endpoints/PayPalPaymentEndpoints.cs")]
    [InlineData(ProjectFrameworkKind.Console, "PayPalDemo.cs")]
    [InlineData(ProjectFrameworkKind.ClassLibrary, "PayPalDemo.cs")]
    public void GetTargetPath_ReturnsExpectedRelativePath_ForScaffoldableKinds(ProjectFrameworkKind kind, string expectedRelativePath)
    {
        using var dir = new TempDirectory();
        var project = new DetectedProject { Kind = kind, ProjectPath = "App.csproj" };

        var path = CodeScaffolder.GetTargetPath(dir.Path, project);

        Assert.NotNull(path);
        Assert.Equal(Path.Combine(dir.Path, expectedRelativePath.Replace('/', Path.DirectorySeparatorChar)), path);
    }

    [Fact]
    public void GetTargetPath_ReturnsNull_ForWorkerServiceAndUnknown()
    {
        using var dir = new TempDirectory();

        Assert.Null(CodeScaffolder.GetTargetPath(dir.Path, new DetectedProject { Kind = ProjectFrameworkKind.WorkerService }));
        Assert.Null(CodeScaffolder.GetTargetPath(dir.Path, new DetectedProject { Kind = ProjectFrameworkKind.Unknown }));
    }

    [Fact]
    public void GetTargetPath_ReturnsNull_ForFileBasedApps_RegardlessOfKind()
    {
        using var dir = new TempDirectory();
        var project = new DetectedProject { Kind = ProjectFrameworkKind.AspNetCoreWeb, IsFileBasedApp = true, ProjectPath = "app.cs" };

        Assert.Null(CodeScaffolder.GetTargetPath(dir.Path, project));
    }

    [Fact]
    public void Scaffold_WritesAspNetCoreEndpointsFile_ContainingExpectedExtensionMethod()
    {
        using var dir = new TempDirectory();
        var project = new DetectedProject { Kind = ProjectFrameworkKind.AspNetCoreWeb, ProjectPath = "App.csproj" };

        var written = CodeScaffolder.Scaffold(dir.Path, project);

        Assert.NotNull(written);
        Assert.True(File.Exists(written));
        var content = File.ReadAllText(written!);
        Assert.Contains("public static class PayPalPaymentEndpoints", content);
        Assert.Contains("MapPayPalPaymentEndpoints", content);
        Assert.DoesNotContain("static void Main", content);
        Assert.DoesNotContain("class Program", content);
    }

    [Fact]
    public void Scaffold_WritesStandaloneDemoFile_ContainingExpectedHelper()
    {
        using var dir = new TempDirectory();
        var project = new DetectedProject { Kind = ProjectFrameworkKind.Console, ProjectPath = "App.csproj" };

        var written = CodeScaffolder.Scaffold(dir.Path, project);

        Assert.NotNull(written);
        var content = File.ReadAllText(written!);
        Assert.Contains("public static class PayPalDemo", content);
        Assert.Contains("RunAsync", content);
        Assert.DoesNotContain("static void Main", content);
        Assert.DoesNotContain("class Program", content);
    }

    [Fact]
    public void Scaffold_ReturnsNull_ForFileBasedApp_AndWritesNothing()
    {
        using var dir = new TempDirectory();
        var project = new DetectedProject { Kind = ProjectFrameworkKind.Console, IsFileBasedApp = true, ProjectPath = "app.cs" };

        var written = CodeScaffolder.Scaffold(dir.Path, project);

        Assert.Null(written);
        Assert.Empty(Directory.GetFiles(dir.Path));
    }

    [Fact]
    public void Scaffold_Overwrites_WhenCalledAgain()
    {
        using var dir = new TempDirectory();
        var project = new DetectedProject { Kind = ProjectFrameworkKind.Console, ProjectPath = "App.csproj" };
        var path = CodeScaffolder.GetTargetPath(dir.Path, project)!;
        File.WriteAllText(path, "// stale placeholder");

        CodeScaffolder.Scaffold(dir.Path, project);

        Assert.Contains("PayPalDemo", File.ReadAllText(path));
    }
}
