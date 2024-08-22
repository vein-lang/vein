namespace vein.cmd;

using Spectre.Console;
using Spectre.Console.Cli;

public static class CommandEx
{
    public static T Ask<T>(this CommandContext cmd, string prompt, T defaultValue, Func<T, ValidationResult>? validator) 
    {
        retry:
        var result = AnsiConsole.Ask<T>(prompt, defaultValue);

        if (validator is null)
            return result;

        var validateErr = validator(result);

        if (validateErr.Successful)
            return result;
        Log.Warn($"[green]Invalid format[/] {validateErr.Message}");
        goto retry;
    }

    public static T Ask<T>(this CommandContext cmd, string prompt, Func<T, ValidationResult>? validator)
    {
        retry:
        var result = AnsiConsole.Ask<T>(prompt);

        if (validator is null)
            return result;
        var validateErr = validator(result);
        if (validateErr.Successful)
            return result;
        Log.Warn($"[green]Invalid format[/] {validateErr.Message}");
        goto retry;
    }

    public static T Ask<T>(this CommandContext cmd, string prompt, T defaultValue, Func<T, bool>? validator)
    {
        retry:
        var result = AnsiConsole.Ask<T>(prompt, defaultValue);

        if (validator is null)
            return result;

        if (validator(result))
            return result;
        Log.Warn($"[green]Invalid format[/]");
        goto retry;
    }

    public static T Ask<T>(this CommandContext cmd, string prompt, Func<T, bool>? validator)
    {
        retry:
        var result = AnsiConsole.Ask<T>(prompt);

        if (validator is null)
            return result;
        if (validator(result))
            return result;
        Log.Warn($"[green]Invalid format[/]");
        goto retry;
    }
}
