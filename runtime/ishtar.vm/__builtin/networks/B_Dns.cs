namespace ishtar.networks;

public static unsafe class B_Dns
{
    public static IshtarObject* not_impl(CallFrame* current, IshtarObject** args)
        => throw new NotImplementedException();

    public static void InitTable(ForeignFunctionInterface ffi)
    {
        ffi.Add("_dns_query_domain_details([std]::std::String) -> [std]::std::Job<T>", ffi.AsNative(&not_impl));
    }
}
