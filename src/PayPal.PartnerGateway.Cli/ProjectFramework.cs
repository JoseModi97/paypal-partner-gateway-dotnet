namespace PayPal.PartnerGateway.Cli;

/// <summary>The programming model <see cref="FrameworkDetector"/> found - decides which package(s) and usage snippet to offer.</summary>
public enum ProjectFrameworkKind
{
    /// <summary>An ASP.NET Core web app (Minimal API or MVC) - has access to <c>IServiceCollection</c> DI.</summary>
    AspNetCoreWeb,

    /// <summary>A .NET Generic Host worker/background service - also has DI via <c>Host.CreateApplicationBuilder</c>.</summary>
    WorkerService,

    /// <summary>A console application (or file-based app with no DI host) with no built-in DI container.</summary>
    Console,

    /// <summary>A class library - the caller decides how it's hosted.</summary>
    ClassLibrary,

    /// <summary>Couldn't confidently classify the project.</summary>
    Unknown,
}

/// <summary>The result of scanning a directory for a .NET project or file-based app.</summary>
public class DetectedProject
{
    /// <summary>Path to the .csproj, or - when <see cref="IsFileBasedApp"/> is true - the single entry .cs file.</summary>
    public string ProjectPath { get; init; } = string.Empty;

    public ProjectFrameworkKind Kind { get; init; }
    public string TargetFramework { get; init; } = "net8.0";

    /// <summary>
    /// True when there's no .csproj at all and this is a .NET 10+ "file-based app"
    /// (<c>dotnet run app.cs</c>) - dependencies go in as <c>#:package</c> directives inside the
    /// file itself rather than via <c>dotnet add package</c>.
    /// </summary>
    public bool IsFileBasedApp { get; init; }

    /// <summary>True for <see cref="ProjectFrameworkKind.AspNetCoreWeb"/> and <see cref="ProjectFrameworkKind.WorkerService"/> - projects with an <c>IServiceCollection</c> to register into.</summary>
    public bool HasDependencyInjection => Kind is ProjectFrameworkKind.AspNetCoreWeb or ProjectFrameworkKind.WorkerService;
}
