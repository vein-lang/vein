namespace vein.compilation;

using System.Linq;
using ishtar.emit;
using runtime;
using syntax;
using static runtime.VeinTypeCode;

public partial class CompilationTask
{
    private void ShitcodePlug((ClassBuilder clazz, MemberDeclarationSyntax member) clz)
        => ShitcodePlug(clz.clazz);

    private void ShitcodePlug(ClassBuilder clz)
    {
        var dd = clz.Parents.FirstOrDefault(x => x.FullName == VeinCore.ValueTypeClass.FullName);
        if (dd is not null && dd != VeinCore.ValueTypeClass)
        {
            clz.Parents.Remove(dd);
            clz.Parents.Add(VeinCore.ValueTypeClass);
        }
        var ss = clz.Parents.FirstOrDefault(x => x.FullName == VeinCore.ObjectClass.FullName);
        if (ss is not null && ss != VeinCore.ObjectClass)
        {
            clz.Parents.Remove(ss);
            clz.Parents.Add(VeinCore.ObjectClass);
        }
    }
}
