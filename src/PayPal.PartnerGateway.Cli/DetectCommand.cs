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
            Console.WriteLine($"No .csproj found in {directory}");
            return 1;
        }

        Console.WriteLine($"Project:               {Path.GetFileName(project.CsprojPath)}");
        Console.WriteLine($"Target framework:      {project.TargetFramework}");
        Console.WriteLine($"Detected kind:         {project.Kind}");
        Console.WriteLine($"Has DI container:      {project.HasDependencyInjection}");
        Console.WriteLine($"Would install:         PayPal.PartnerGateway{(project.HasDependencyInjection ? " + PayPal.PartnerGateway.AspNetCore" : "")}");
        return 0;
    }
}
