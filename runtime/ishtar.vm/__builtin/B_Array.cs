namespace ishtar;

public static unsafe class B_Array
{
    public static IshtarObject* __indexer_getter(CallFrame* current, IshtarObject** args)
        => throw new NotImplementedException();

    public static void InitTable(ForeignFunctionInterface ffi)
    {
        ffi.Add("__indexer_getter([std]::std::Object,[std]::std::Int32) -> [std]::std::Object", ffi.AsNative(&__indexer_getter));
    }
}
