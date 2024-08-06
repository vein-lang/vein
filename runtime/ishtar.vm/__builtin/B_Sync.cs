namespace ishtar;
public static unsafe class B_Sync
{
    public static IshtarObject* not_impl(CallFrame* current, IshtarObject** args)
        => throw new NotImplementedException();

    public static void InitTable(ForeignFunctionInterface ffi)
    {
        ffi.Add("sync_create_semaphore() -> [std]::std::Raw", ffi.AsNative(&not_impl));
        ffi.Add("sync_create_mutex() -> [std]::std::Raw", ffi.AsNative(&not_impl));

        ffi.Add("sync_semaphore_wait([std]::std::Raw) -> [std]::std::Void", ffi.AsNative(&not_impl));
        ffi.Add("sync_semaphore_post([std]::std::Raw) -> [std]::std::Void", ffi.AsNative(&not_impl));

        ffi.Add("sync_mutex_lock([std]::std::Raw) -> [std]::std::Void", ffi.AsNative(&not_impl));
        ffi.Add("sync_mutex_unlock([std]::std::Raw) -> [std]::std::Void", ffi.AsNative(&not_impl));
    }
}

//
