namespace vein;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using compilation;
using ishtar;
using MoreLinq;
using syntax;
using static Spectre.Console.AnsiConsole;

public static class Log
{
    public static Queue<string> warnings { get; } = new();
    public static Queue<string> errors { get; } = new();
    public static Queue<string> infos { get; } = new();

    public static class Defer
    {
        public static void Warn(string text) => _print(text, null, null, warnings);
        public static void Warn(string text, BaseSyntax posed) => _print(text, posed, null, warnings);
        public static void Warn(string text, BaseSyntax posed, DocumentDeclaration doc)
            => _print(text, posed, doc, warnings);

        public static void Error(string text) => _print(text, null, null, errors);
        public static void Error(string text, BaseSyntax posed) => _print(text, posed, null, errors);
        public static void Error(string text, BaseSyntax posed, DocumentDeclaration doc)
            => _print(text, posed, doc, errors);

        public static void Info(string text) => _print(text, null, null, infos);
        public static void Info(string text, BaseSyntax posed) => _print(text, posed, null, infos);
        public static void Info(string text, BaseSyntax posed, DocumentDeclaration doc)
            => _print(text, posed, doc, infos);

    }

    public static void EnqueueErrorsRange(IEnumerable<string> s) => s.Pipe(x => errors.Enqueue(x)).Consume();
    public static void EnqueueInfosRange(IEnumerable<string> s) => s.Pipe(x => infos.Enqueue(x)).Consume();
    public static void EnqueueWarnsRange(IEnumerable<string> s) => s.Pipe(x => warnings.Enqueue(x)).Consume();


    public static void Info(string s) => MarkupLine($"[aqua]INFO[/]: {s}");
    public static void Warn(string s) => MarkupLine($"[orange]WARN[/]: {s}");
    public static void Error(string s) => MarkupLine($"[red]ERROR[/]: {s}");
    public static void Error(Exception s) => WriteException(s);

    public static void Info(string s, CompilationTarget t) => t.Logs.Info.Enqueue($"[aqua]INFO[/]: {s}");
    public static void Warn(string s, CompilationTarget t) => t.Logs.Warn.Enqueue($"[orange]WARN[/]: {s}");
    public static void Error(string s, CompilationTarget t) => t.Logs.Error.Enqueue($"[red]ERROR[/]: {s}");

    private static void _print(string text, BaseSyntax posed, DocumentDeclaration doc, Queue<string> queue)
    {
        if (posed is { Transform: null })
        {
            errors.Enqueue($"INTERNAL ERROR: TOKEN '{posed.GetType().Name}' HAS INCORRECT TRANSFORM POSITION.");
            return;
        }

        var strBuilder = new StringBuilder();

        strBuilder.Append($"{text.EscapeArgumentSymbols()}\n");



        if (posed is not null)
        {
            strBuilder.Append(
                $"\tat '[orange bold]{posed.Transform.pos.Line} line, {posed.Transform.pos.Column} column[/]' \n");
        }

        if (doc is not null)
        {
            strBuilder.Append(
                $"\tin '[orange bold]{doc.FileEntity}[/]'.");
        }

        if (posed is not null && doc is not null)
        {
            var diff_err = posed.Transform.DiffErrorFull(doc);
            strBuilder.Append($"\t\t{diff_err}");
        }

        queue.Enqueue(strBuilder.ToString());
    }
}
