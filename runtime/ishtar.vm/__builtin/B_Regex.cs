namespace ishtar;

public static unsafe class B_Regex
{
    public static IshtarObject* not_implemented(CallFrame* current, IshtarObject** args)
        => throw new NotImplementedException();


    public static void InitTable(ForeignFunctionInterface ffi)
    {
        ffi.Add("regex_replace([std]::std::String,[std]::std::String,[std]::std::String) -> [std]::std::Boolean",
            ffi.AsNative(&not_implemented));
    }
}
