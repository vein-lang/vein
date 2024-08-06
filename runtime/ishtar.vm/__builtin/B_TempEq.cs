namespace ishtar;

public static unsafe class B_TempEq
{
    public static IshtarObject* not_impl(CallFrame* current, IshtarObject** args)
        => throw new NotImplementedException();

    public static void InitTable(ForeignFunctionInterface ffi)
    {
        ffi.Add("@__sys_eql_2T([std]::std::Object,[std]::std::Object) -> [std]::std::Boolean", ffi.AsNative(&not_impl));
    }
}
