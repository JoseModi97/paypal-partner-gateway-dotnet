using System;
using System.IO;

namespace PayPal.PartnerGateway.Cli;

/// <summary>Implements <c>paypal-partner detect</c>: prints what <c>init</c> would detect, without changing anything.</summary>
public static class DetectCommand
{
    public static int Run()
    {
        var directory = Directory.GetCurrentDirectory();
        var project = FrameworkDetector.Detect(directory);

        if (project == null)
        {
            Console.WriteLine($"No .csproj and no single .cs file found in {directory}.");
            Console.WriteLine("Run this from a project's root folder, or a folder with one file-based app (e.g. app.cs).");
            return 1;
        }

        var installMethod = project.IsFileBasedApp ? "#:package directive" : "dotnet add package";

        Console.WriteLine($"Entry point:           {Path.GetFileName(project.ProjectPath)}{(project.IsFileBasedApp ? " (file-based app, no .csproj)" : "")}");
        Console.WriteLine($"Target framework:      {project.TargetFramework}");
        Console.WriteLine($"Detected kind:         {project.Kind}");
        Console.WriteLine($"Has DI container:      {project.HasDependencyInjection}");
        Console.WriteLine($"Install method:        {installMethod}");
        Console.WriteLine($"Would install:         PayPal.PartnerGateway{(project.HasDependencyInjection ? " + PayPal.PartnerGateway.AspNetCore" : "")}");
        return 0;
    }
}
