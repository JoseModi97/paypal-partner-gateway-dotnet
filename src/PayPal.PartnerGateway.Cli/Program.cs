using System;
using System.Reflection;
using System.Threading.Tasks;
using PayPal.PartnerGateway.Cli;

if (args.Length > 0 && args[0] is "-h" or "--help" or "help")
{
    PrintHelp();
    return 0;
}

if (args.Length > 0 && args[0] is "-v" or "--version" or "version")
{
    var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
    Console.WriteLine($"paypal-partner v{version}");
    return 0;
}

PrintBanner();

string command;
string[] rest;

if (args.Length == 0)
{
    // No command given - show a menu instead of guessing. An explicit command
    // ('paypal-partner init', 'paypal-partner detect', ...) always skips this and runs directly.
    command = Prompter.Select("What would you like to do?", new (string, string)[]
    {
        ("Set up this project (init)", "init"),
        ("Detect project type only, don't change anything (detect)", "detect"),
        ("Exit", "exit"),
    }, defaultIndex: 0);
    rest = Array.Empty<string>();
    Console.WriteLine();

    if (command == "exit")
    {
        return 0;
    }
}
else
{
    command = args[0];
    rest = args.Length > 1 ? args[1..] : Array.Empty<string>();
}

try
{
    return command.ToLowerInvariant() switch
    {
        "init" => await InitCommand.RunAsync(rest),
        "detect" => DetectCommand.Run(),
        _ => Unknown(command),
    };
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\nError: {ex.Message}");
    Console.ResetColor();
    return 1;
}

static int Unknown(string cmd)
{
    Console.Error.WriteLine($"Unknown command '{cmd}'.\n");
    PrintHelp();
    return 1;
}

static void PrintBanner()
{
    Console.WriteLine("=========================================================");
    Console.WriteLine("        PayPal Partner Gateway Setup Wizard (CLI)");
    Console.WriteLine("=========================================================");
}

static void PrintHelp()
{
    Console.WriteLine(@"
paypal-partner - setup helper for PayPal.PartnerGateway

Usage:
  paypal-partner                 Show an interactive menu (init / detect / exit)
  paypal-partner init            Run the setup wizard directly
  paypal-partner detect          Show what 'init' would detect and install, without changing anything
  paypal-partner --help          Show this help
  paypal-partner --version       Show version

'init' auto-detects your project (ASP.NET Core, Worker Service, console app, class library, or a
.NET 10+ file-based app with no .csproj) - it never asks which one you're in. It then interactively
walks you through your PayPal credentials and optional partner settings, adds the matching
PayPal.PartnerGateway package(s) the way your project actually consumes packages ('dotnet add package',
or a #:package directive for a file-based app), wires appsettings.json for DI-capable hosts, and
scaffolds a starter usage file.

Re-running 'init' is safe: it detects a prior run (existing appsettings.json PayPalPartner section, or
an existing #:package directive), pre-fills those values as defaults, and asks before overwriting
anything that already exists (the generated starter file, or a differing appsettings.json section)
instead of silently clobbering it.

Options for 'init':
  --client-id <id>                  PayPal Client ID (skips that prompt)
  --client-secret <secret>          PayPal Client Secret (skips that prompt)
  --environment <Sandbox|Live>      Target environment (skips that prompt)
  --partner-attribution-id <bn>     PayPal-Partner-Attribution-Id / BN code (optional)
  --webhook-id <id>                 Webhook ID from the Webhooks tab (optional)
  --yes, -y                         Skip all interactive prompts - use flags/environment variables only
  --force                           Overwrite an existing starter file without asking
  --dry-run                         Show what would happen, without changing anything
");
}
