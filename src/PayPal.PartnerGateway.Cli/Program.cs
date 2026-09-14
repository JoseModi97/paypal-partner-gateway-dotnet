using System;
using System.Threading.Tasks;
using PayPal.PartnerGateway.Cli;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    PrintHelp();
    return 0;
}

return args[0] switch
{
    "init" => await InitCommand.RunAsync(args[1..]),
    "detect" => DetectCommand.Run(),
    _ => Unknown(args[0]),
};

static int Unknown(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'.");
    PrintHelp();
    return 1;
}

static void PrintHelp()
{
    Console.WriteLine("paypal-partner - setup helper for PayPal.PartnerGateway");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  paypal-partner init [--client-id ID] [--client-secret SECRET] [--environment Sandbox|Live] [--dry-run]");
    Console.WriteLine("      Detects the .NET project in the current directory (ASP.NET Core, Worker Service,");
    Console.WriteLine("      console app, or class library), adds the matching PayPal.PartnerGateway package(s)");
    Console.WriteLine("      via 'dotnet add package', wires appsettings.json for DI-capable hosts, and prints");
    Console.WriteLine("      a ready-to-paste usage snippet for the detected framework.");
    Console.WriteLine();
    Console.WriteLine("  paypal-partner detect");
    Console.WriteLine("      Shows what 'init' would detect and install, without changing anything.");
}
