using System;
using System.Collections.Generic;

namespace PayPal.PartnerGateway.Cli;

/// <summary>Small console-prompting helpers used by the interactive <c>init</c> wizard.</summary>
public static class Prompter
{
    /// <summary>Asks for a line of text, showing <paramref name="defaultValue"/> as the value Enter accepts.</summary>
    public static string Ask(string message, string? defaultValue = null, Func<string, bool>? validate = null, string? validationMessage = null)
    {
        while (true)
        {
            Console.Write(!string.IsNullOrEmpty(defaultValue) ? $"{message} [{defaultValue}]: " : $"{message}: ");

            var input = Console.ReadLine();
            var value = string.IsNullOrWhiteSpace(input) ? (defaultValue ?? string.Empty) : input.Trim();

            if (validate == null || validate(value))
            {
                return value;
            }

            WriteError(validationMessage ?? "Invalid input. Please try again.");
        }
    }

    /// <summary>Like <see cref="Ask"/>, but masks each typed character with '*' (for secrets).</summary>
    public static string AskSecret(string message, string? defaultValue = null, Func<string, bool>? validate = null, string? validationMessage = null)
    {
        while (true)
        {
            Console.Write(!string.IsNullOrEmpty(defaultValue) ? $"{message} [****]: " : $"{message}: ");

            var input = ReadMasked();
            var value = string.IsNullOrWhiteSpace(input) ? (defaultValue ?? string.Empty) : input.Trim();

            if (validate == null || validate(value))
            {
                return value;
            }

            WriteError(validationMessage ?? "Invalid input. Please try again.");
        }
    }

    /// <summary>Asks a yes/no question.</summary>
    public static bool Confirm(string message, bool defaultYes = true)
    {
        var hint = defaultYes ? "[Y/n]" : "[y/N]";
        Console.Write($"{message} {hint}: ");
        var input = Console.ReadLine()?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(input)) return defaultYes;
        return input is "y" or "yes" or "true" or "1";
    }

    /// <summary>Presents a numbered menu and returns the selected choice's value.</summary>
    public static string Select(string message, IReadOnlyList<(string Label, string Value)> choices, int defaultIndex = 0)
    {
        Console.WriteLine(message);
        for (var i = 0; i < choices.Count; i++)
        {
            var marker = i == defaultIndex ? " (default)" : "";
            Console.WriteLine($"  {i + 1}) {choices[i].Label}{marker}");
        }

        while (true)
        {
            Console.Write($"Enter selection (1-{choices.Count}) [{defaultIndex + 1}]: ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input)) return choices[defaultIndex].Value;
            if (int.TryParse(input, out var choice) && choice >= 1 && choice <= choices.Count) return choices[choice - 1].Value;

            WriteError($"Please enter a number between 1 and {choices.Count}.");
        }
    }

    private static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static string ReadMasked()
    {
        // Console.ReadKey requires a real console - fall back to a plain (unmasked) read when
        // stdin is redirected, e.g. piped input, CI, or a container with no TTY attached.
        if (Console.IsInputRedirected)
        {
            return Console.ReadLine() ?? string.Empty;
        }

        var value = string.Empty;
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (value.Length > 0)
                {
                    value = value[..^1];
                    Console.Write("\b \b");
                }
                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                value += key.KeyChar;
                Console.Write("*");
            }
        }

        return value;
    }
}
