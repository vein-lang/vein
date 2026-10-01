namespace ishtar;

using System.Collections.Generic;
using collections;
using runtime.gc;

public unsafe partial struct VirtualMachine
{
    /// <summary>
    /// Reified type arguments for instantiated generic objects (v1 side table).
    /// Keyed by object pointer -> the concrete type-argument <c>types_table</c> indices (the same
    /// indices LD_TYPE carries), in declaration order. Populated by NEWOBJ when it pops the
    /// type-argument tokens that <c>EmitGenericsLoad</c> pushed; read by LD_TYPE_G to resolve a class
    /// type parameter against the receiver. Objects are non-moving (conservative GC) so the pointer key
    /// is stable. TODO: move this onto IshtarObject and free with the object instead of leaking; store
    /// resolved class handles (not module-local indices) once cross-module generics are needed.
    /// </summary>
    internal static readonly Dictionary<nint, int[]> GenericObjectArgs = new();

    /// <summary>
    /// Instantiation cache: interns one canonical closed-type handle per (open definition + ordered type
    /// arguments), so <c>List&lt;i32&gt;</c> always maps to the SAME <see cref="RuntimeComplexType"/> pointer
    /// and <c>List&lt;string&gt;</c> to a different one. This gives generic instantiations a stable identity
    /// (pointer equality) for <c>is</c> / <c>cast</c> / <c>typeof</c>, without cloning the class. Keyed by
    /// the rendered closed-type template (e.g. <c>"[std]::std::List&lt;Int32&gt;"</c>).
    /// TODO: this leaks the interned handles until VM dispose; free per closed type once ownership is clear.
    /// </summary>
    internal static readonly Dictionary<string, nint> GenericInstanceCache = new();

    /// <summary>
    /// Returns the canonical closed-type handle for <paramref name="definition"/> instantiated with
    /// <paramref name="args"/>, creating and caching it on first use. Identical (definition, args) always
    /// yields the SAME pointer, so two closed types are the same instantiation iff their handles are equal.
    /// </summary>
    internal static RuntimeComplexType* InstantiateGeneric(RuntimeIshtarClass* definition,
        NativeList<RuntimeComplexType>* args)
    {
        var probe = new RuntimeComplexType(definition, args);
        var key = probe.ToTemplateString();

        if (GenericInstanceCache.TryGetValue(key, out var cached))
            return (RuntimeComplexType*)cached;

        var handle = IshtarGC.AllocateImmortal<RuntimeComplexType>(null);
        *handle = probe;
        GenericInstanceCache[key] = (nint)handle;
        return handle;
    }

    /// <summary>
    /// True when two closed generic types denote the same instantiation (same open definition and,
    /// invariantly in v1, the same type arguments). Handles obtained from <see cref="InstantiateGeneric"/>
    /// compare by pointer; structurally-equal inputs are also accepted.
    /// </summary>
    internal static bool SameGenericInstance(RuntimeComplexType* a, RuntimeComplexType* b)
        => a == b || RuntimeComplexType.Eq(a, b);
}
