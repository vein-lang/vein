namespace vein.compilation;

using System.Linq;
using ishtar.emit;
using syntax;
using static runtime.VeinTypeCode;

public partial class CompilationTask
{
    public void ValidateInheritance((ClassBuilder @class, MemberDeclarationSyntax member) x)
    {
        if (x.member is not ClassDeclarationSyntax member)
            return;
        var (@class, _) = x;
        ValidateInheritanceInterfaces(@class, member);
        ValidateCollisionsMethods(@class, member);
    }

    public void ValidateInheritanceInterfaces(ClassBuilder @class, ClassDeclarationSyntax member)
    {
        var prepairedAbstracts = @class.Parents
            .SelectMany(x => x.Methods)
            .Where(x => !x.IsPrivate)
            .Where(x => !x.IsStatic)
            .Where(x => x.IsAbstract);

        foreach (var method in prepairedAbstracts.Where(x => !@class.ContainsImpl(x)))
        {
            Log.Defer.Error(
                $"[red]'{@class.Name}'[/] does not implement inherited abstract member [red]'{method.Owner.Name}.{method.Name}'[/]"
                , member.Identifier, member.OwnerDocument);
        }
    }
    public void ValidateCollisionsMethods(ClassBuilder @class, ClassDeclarationSyntax member)
    {
        var prepairedOthers = @class.Parents
            .SelectMany(x => x.Methods)
            .Where(x => !x.IsPrivate)
            .Where(x => !x.IsAbstract)
            .Where(x => !x.IsSpecial);

        foreach (var method in prepairedOthers.Where(x => @class.Methods.Any(z => !z.IsOverride && z.Name == x.Name)))
        {
            var pos = member.Methods.FirstOrDefault(x => x.IsEquals(method));

            Log.Defer.Warn(
                $"[yellow]'{method.Owner.Name}::{method.Name}' hides inherited member '{member.Identifier}::{method.Name}'.[/]",
                pos.Identifier, member.OwnerDocument);
        }
    }
}
