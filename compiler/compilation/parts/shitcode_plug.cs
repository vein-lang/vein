namespace vein.compilation;

using System.Linq;
using ishtar;
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
        var dd = clz.Parents.FirstOrDefault(x => x.FullName == Types.Storage.ValueTypeClass.FullName);
        if (dd is not null && dd != Types.Storage.ValueTypeClass)
        {
            clz.Parents.Remove(dd);
            clz.Parents.Add(Types.Storage.ValueTypeClass);
        }
        var ss = clz.Parents.FirstOrDefault(x => x.FullName == Types.Storage.ObjectClass.FullName);
        if (ss is not null && ss != Types.Storage.ObjectClass)
        {
            clz.Parents.Remove(ss);
            clz.Parents.Add(Types.Storage.ObjectClass);
        }
    }
}
