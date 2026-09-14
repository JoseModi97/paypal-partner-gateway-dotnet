using Xunit;

namespace PayPal.PartnerGateway.Cli.Tests;

public class FrameworkDetectorTests
{
    [Fact]
    public void Detect_ReturnsNull_WhenDirectoryHasNoProjectOrFile()
    {
        using var dir = new TempDirectory();

        var result = FrameworkDetector.Detect(dir.Path);

        Assert.Null(result);
    }

    [Fact]
    public void Detect_ClassifiesAspNetCoreWeb_FromSdkWebAttribute()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");

        var result = FrameworkDetector.Detect(dir.Path);

        Assert.NotNull(result);
        Assert.Equal(ProjectFrameworkKind.AspNetCoreWeb, result!.Kind);
        Assert.True(result.HasDependencyInjection);
        Assert.False(result.IsFileBasedApp);
        Assert.Equal("net8.0", result.TargetFramework);
    }

    [Fact]
    public void Detect_ClassifiesConsole_FromOutputTypeExe()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");

        var result = FrameworkDetector.Detect(dir.Path);

        Assert.Equal(ProjectFrameworkKind.Console, result!.Kind);
        Assert.False(result.HasDependencyInjection);
    }

    [Fact]
    public void Detect_ClassifiesClassLibrary_ForPlainSdkWithNoOutputType()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");

        var result = FrameworkDetector.Detect(dir.Path);

        Assert.Equal(ProjectFrameworkKind.ClassLibrary, result!.Kind);
    }

    [Fact]
    public void Detect_ClassifiesWorkerService_FromSdkWorkerAttribute()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk.Worker\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");

        var result = FrameworkDetector.Detect(dir.Path);

        Assert.Equal(ProjectFrameworkKind.WorkerService, result!.Kind);
        Assert.True(result.HasDependencyInjection);
    }

    [Fact]
    public void Detect_UsesProgramCsContent_WhenCsprojIsAmbiguous()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
        dir.WriteFile("Program.cs", "var builder = WebApplication.CreateBuilder(args);\nvar app = builder.Build();\napp.Run();");

        var result = FrameworkDetector.Detect(dir.Path);

        Assert.Equal(ProjectFrameworkKind.AspNetCoreWeb, result!.Kind);
    }

    [Fact]
    public void Detect_FindsFileBasedApp_WhenNoCsprojButOneCsFileExists()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("app.cs", "Console.WriteLine(\"hi\");");

        var result = FrameworkDetector.Detect(dir.Path);

        Assert.NotNull(result);
        Assert.True(result!.IsFileBasedApp);
        Assert.Equal(ProjectFrameworkKind.Console, result.Kind);
        Assert.Equal("net10.0", result.TargetFramework);
    }

    [Fact]
    public void Detect_FindsAspNetCoreFileBasedApp_FromWebApplicationCreateBuilder()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("app.cs", "var builder = WebApplication.CreateBuilder(args);\nvar app = builder.Build();\napp.Run();");

        var result = FrameworkDetector.Detect(dir.Path);

        Assert.True(result!.IsFileBasedApp);
        Assert.Equal(ProjectFrameworkKind.AspNetCoreWeb, result.Kind);
        Assert.True(result.HasDependencyInjection);
    }

    [Fact]
    public void Detect_FindsAspNetCoreFileBasedApp_FromSdkWebDirective()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("app.cs", "#:sdk Microsoft.NET.Sdk.Web\nConsole.WriteLine(\"hi\");");

        var result = FrameworkDetector.Detect(dir.Path);

        Assert.True(result!.IsFileBasedApp);
        Assert.Equal(ProjectFrameworkKind.AspNetCoreWeb, result.Kind);
    }

    [Fact]
    public void Detect_PrefersAppCsOverAmbiguousMultipleFiles()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("app.cs", "Console.WriteLine(\"main\");");
        dir.WriteFile("Helper.cs", "class Helper {}");

        var result = FrameworkDetector.Detect(dir.Path);

        Assert.NotNull(result);
        Assert.EndsWith("app.cs", result!.ProjectPath);
    }

    [Fact]
    public void Detect_PrefersCsprojOverStrayCsFiles()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
        dir.WriteFile("Program.cs", "// top-level statements");

        var result = FrameworkDetector.Detect(dir.Path);

        Assert.False(result!.IsFileBasedApp);
        Assert.EndsWith("App.csproj", result.ProjectPath);
    }
}
