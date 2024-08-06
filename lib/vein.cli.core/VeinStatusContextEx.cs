namespace vein.compilation;

using System;
using System.Threading;
using project;
using Spectre.Console;
using styles;

public static class VeinStatusContextEx
{
    /// <summary>Sets the status message.</summary>
    /// <param name="context">The status context.</param>
    /// <param name="status">The status message.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static ProgressTask VeinStatus(this ProgressTask context, string status)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        Thread.Sleep(10); // so, i need it :(

        if (context.State.Get<bool>("isDeps"))
            context.Description = $"[red](dep)[/] {status}";
        if (context.State.Get<VeinProject>("project") is { } p)
            context.Description = $"[orange](project [purple]{p.Name}[/])[/] {status}";
        else
            context.Description = status;
        return context;
    }
}
