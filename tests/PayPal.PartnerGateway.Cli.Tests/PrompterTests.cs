using System;
using System.IO;
using Xunit;

namespace PayPal.PartnerGateway.Cli.Tests;

/// <summary>
/// Drives Prompter through redirected Console.In/Out. Since these tests already run with stdin
/// redirected (true of any 'dotnet test' process), AskSecret naturally exercises its
/// Console.IsInputRedirected fallback path here rather than the ReadKey masking path.
/// </summary>
public class PrompterTests : IDisposable
{
    private readonly TextReader _originalIn = Console.In;
    private readonly TextWriter _originalOut = Console.Out;

    public void Dispose()
    {
        Console.SetIn(_originalIn);
        Console.SetOut(_originalOut);
    }

    private static void SetInput(string input) => Console.SetIn(new StringReader(input));

    [Fact]
    public void Ask_ReturnsTypedValue()
    {
        SetInput("hello\n");
        Console.SetOut(TextWriter.Null);

        var result = Prompter.Ask("Name");

        Assert.Equal("hello", result);
    }

    [Fact]
    public void Ask_ReturnsDefault_WhenInputIsBlank()
    {
        SetInput("\n");
        Console.SetOut(TextWriter.Null);

        var result = Prompter.Ask("Name", "fallback");

        Assert.Equal("fallback", result);
    }

    [Fact]
    public void Ask_Retries_UntilValidationPasses()
    {
        SetInput("\nstill-blank\nok\n");
        Console.SetOut(TextWriter.Null);

        var result = Prompter.Ask("Name", validate: s => s == "ok");

        Assert.Equal("ok", result);
    }

    [Fact]
    public void AskSecret_ReadsValue_ViaRedirectedInputFallback()
    {
        SetInput("super-secret\n");
        Console.SetOut(TextWriter.Null);

        var result = Prompter.AskSecret("Secret");

        Assert.Equal("super-secret", result);
        Assert.True(Console.IsInputRedirected);
    }

    [Fact]
    public void Confirm_DefaultsToTrue_WhenInputIsBlankAndDefaultYesIsTrue()
    {
        SetInput("\n");
        Console.SetOut(TextWriter.Null);

        Assert.True(Prompter.Confirm("Proceed?", defaultYes: true));
    }

    [Fact]
    public void Confirm_DefaultsToFalse_WhenInputIsBlankAndDefaultYesIsFalse()
    {
        SetInput("\n");
        Console.SetOut(TextWriter.Null);

        Assert.False(Prompter.Confirm("Proceed?", defaultYes: false));
    }

    [Theory]
    [InlineData("y", true)]
    [InlineData("yes", true)]
    [InlineData("n", false)]
    [InlineData("no", false)]
    public void Confirm_ParsesExplicitAnswers(string typed, bool expected)
    {
        SetInput(typed + "\n");
        Console.SetOut(TextWriter.Null);

        Assert.Equal(expected, Prompter.Confirm("Proceed?", defaultYes: !expected));
    }

    [Fact]
    public void Select_ReturnsDefault_WhenInputIsBlank()
    {
        SetInput("\n");
        Console.SetOut(TextWriter.Null);

        var result = Prompter.Select("Pick one:", new (string, string)[] { ("Sandbox", "Sandbox"), ("Live", "Live") }, defaultIndex: 0);

        Assert.Equal("Sandbox", result);
    }

    [Fact]
    public void Select_ReturnsChosenValue_ByNumber()
    {
        SetInput("2\n");
        Console.SetOut(TextWriter.Null);

        var result = Prompter.Select("Pick one:", new (string, string)[] { ("Sandbox", "Sandbox"), ("Live", "Live") }, defaultIndex: 0);

        Assert.Equal("Live", result);
    }

    [Fact]
    public void Select_Retries_OnOutOfRangeSelection()
    {
        SetInput("9\n1\n");
        Console.SetOut(TextWriter.Null);

        var result = Prompter.Select("Pick one:", new (string, string)[] { ("Sandbox", "Sandbox"), ("Live", "Live") }, defaultIndex: 1);

        Assert.Equal("Sandbox", result);
    }
}
