namespace ishtar;

public static unsafe class B_Async
{
    public static IshtarObject* _not_impl(CallFrame* current, IshtarObject** args)
        => throw new NotImplementedException();

    public static void InitTable(ForeignFunctionInterface ffi)
    {
        ffi.Add("_awaitable_call([std]::std::Object) -> [std]::std::Object", ffi.AsNative(&_not_impl));
    }
}
