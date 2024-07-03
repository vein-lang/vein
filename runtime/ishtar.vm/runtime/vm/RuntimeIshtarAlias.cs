namespace ishtar;

using System.Runtime.InteropServices;
using collections;
using runtime;
using runtime.gc;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct RuntimeIshtarAlias : IEq<RuntimeIshtarAlias>
{
    [FieldOffset(0)]
    public AliasKind Kind;
    [FieldOffset(1)]
    public RuntimeIshtarAlias_Method Method;
    [FieldOffset(1)]
    public RuntimeIshtarAlias_Type Type;

    public static RuntimeIshtarAlias* CreateTypeAlias(RuntimeQualityTypeName* name, RuntimeIshtarClass* type)
    {
        var alias = IshtarGC.AllocateImmortal<RuntimeIshtarAlias>(type);

        *alias = new RuntimeIshtarAlias();

        alias->Kind = AliasKind.TYPE;
        alias->Type.Class = type;
        alias->Type.Name = name;

        return alias;
    }

    public static RuntimeIshtarAlias* CreateMethodAlias(RuntimeQualityTypeName* name, RuntimeIshtarSignature* type)
    {
        var alias = IshtarGC.AllocateImmortal<RuntimeIshtarAlias>(type);

        *alias = new RuntimeIshtarAlias();

        alias->Kind = AliasKind.METHOD;
        alias->Method.Method = type;
        alias->Method.Name = name;

        return alias;
    }

    public static void Free(RuntimeIshtarAlias* alias) => IshtarGC.FreeImmortal(alias);
    public static bool Eq(RuntimeIshtarAlias* p1, RuntimeIshtarAlias* p2)
    {
        if (p1->Kind != p2->Kind)
            return false;
        if (p1->Kind == AliasKind.METHOD)
            return RuntimeQualityTypeName.Eq(p1->Method.Name, p2->Method.Name);
        if (p1->Kind == AliasKind.TYPE)
            return RuntimeQualityTypeName.Eq(p1->Type.Name, p2->Type.Name);
        return false;
    }
}

public enum AliasKind : byte
{
    TYPE,
    METHOD
}

public unsafe struct RuntimeIshtarAlias_Method
{
    public RuntimeQualityTypeName* Name;
    public RuntimeIshtarSignature* Method;
}

public unsafe struct RuntimeIshtarAlias_Type
{
    public RuntimeQualityTypeName* Name;
    public RuntimeIshtarClass* Class;
}
