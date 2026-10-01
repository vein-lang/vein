namespace ishtar;

using static vein.runtime.VeinTypeCode;

public static unsafe class B_TempEq
{
    /// <summary>
    /// Structural equality of two boxed values (used by <c>List&lt;T&gt;.indexOf</c>/<c>Remove</c>).
    /// Value types compare by their unboxed value, strings by content, everything else by reference.
    /// </summary>
    public static IshtarObject* sys_eql(CallFrame* frame, IshtarObject** args)
    {
        var a = args[0];
        var b = args[1];
        var gc = frame->GetGC();

        if (a == b)
            return gc->ToIshtarObject(true, frame);
        if (a is null || b is null)
            return gc->ToIshtarObject(false, frame);
        if (a->clazz->TypeCode != b->clazz->TypeCode)
            return gc->ToIshtarObject(false, frame);

        var eq = a->clazz->TypeCode switch
        {
            TYPE_BOOLEAN => IshtarMarshal.ToDotnetBoolean(a, frame) == IshtarMarshal.ToDotnetBoolean(b, frame),
            TYPE_CHAR    => IshtarMarshal.ToDotnetChar(a, frame) == IshtarMarshal.ToDotnetChar(b, frame),
            TYPE_I1      => IshtarMarshal.ToDotnetInt8(a, frame) == IshtarMarshal.ToDotnetInt8(b, frame),
            TYPE_U1      => IshtarMarshal.ToDotnetUInt8(a, frame) == IshtarMarshal.ToDotnetUInt8(b, frame),
            TYPE_I2      => IshtarMarshal.ToDotnetInt16(a, frame) == IshtarMarshal.ToDotnetInt16(b, frame),
            TYPE_I4      => IshtarMarshal.ToDotnetInt32(a, frame) == IshtarMarshal.ToDotnetInt32(b, frame),
            TYPE_I8      => IshtarMarshal.ToDotnetInt64(a, frame) == IshtarMarshal.ToDotnetInt64(b, frame),
            TYPE_U2      => IshtarMarshal.ToDotnetUInt16(a, frame) == IshtarMarshal.ToDotnetUInt16(b, frame),
            TYPE_U4      => IshtarMarshal.ToDotnetUInt32(a, frame) == IshtarMarshal.ToDotnetUInt32(b, frame),
            TYPE_U8      => IshtarMarshal.ToDotnetUInt64(a, frame) == IshtarMarshal.ToDotnetUInt64(b, frame),
            TYPE_R2      => IshtarMarshal.ToDotnetHalf(a, frame).Equals(IshtarMarshal.ToDotnetHalf(b, frame)),
            TYPE_R4      => IshtarMarshal.ToDotnetFloat(a, frame).Equals(IshtarMarshal.ToDotnetFloat(b, frame)),
            TYPE_R8      => IshtarMarshal.ToDotnetDouble(a, frame).Equals(IshtarMarshal.ToDotnetDouble(b, frame)),
            TYPE_STRING  => IshtarMarshal.ToDotnetString(a, frame) == IshtarMarshal.ToDotnetString(b, frame),
            _            => a == b,
        };

        return gc->ToIshtarObject(eq, frame);
    }

    public static void InitTable(ForeignFunctionInterface ffi)
    {
        ffi.Add("@__sys_eql_2T([std]::std::Object,[std]::std::Object) -> [std]::std::Boolean", ffi.AsNative(&sys_eql));
    }
}
