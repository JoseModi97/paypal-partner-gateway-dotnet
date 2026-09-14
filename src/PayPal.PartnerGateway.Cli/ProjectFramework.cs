namespace PayPal.PartnerGateway.Cli;

/// <summary>The kind of .NET project <see cref="FrameworkDetector"/> found in a directory.</summary>
public enum ProjectFrameworkKind
{
    /// <summary>An ASP.NET Core web app (Minimal API or MVC) - has access to <c>IServiceCollection</c> DI.</summary>
    AspNetCoreWeb,

    /// <summary>A .NET Generic Host worker/background service - also has DI via <c>Host.CreateApplicationBuilder</c>.</summary>
    WorkerService,

    /// <summary>A console application with no built-in DI container.</summary>
    Console,

    /// <summary>A class library - the caller decides how it's hosted.</summary>
    ClassLibrary,

    /// <summary>Couldn't confidently classify the project.</summary>
    Unknown,
}

/// <summary>The result of scanning a directory for a .NET project.</summary>
public class DetectedProject
{
    public string CsprojPath { get; init; } = string.Empty;
    public ProjectFrameworkKind Kind { get; init; }
    public string TargetFramework { get; init; } = "net8.0";

    /// <summary>True for <see cref="ProjectFrameworkKind.AspNetCoreWeb"/> and <see cref="ProjectFrameworkKind.WorkerService"/> - projects with an <c>IServiceCollection</c> to register into.</summary>
    public bool HasDependencyInjection => Kind is ProjectFrameworkKind.AspNetCoreWeb or ProjectFrameworkKind.WorkerService;
}
