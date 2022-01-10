namespace ishtar.debugger.directives;

using System.Text;

internal interface IDirective
{
    string Name { get; }
    bool Execute(string[] args, StringBuilder output);
    object ParseArgs(string[] args);
}
