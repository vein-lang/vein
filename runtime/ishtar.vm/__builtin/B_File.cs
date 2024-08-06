namespace ishtar;

using static vein.runtime.MethodFlags;

public static unsafe class B_File
{

    public static IshtarObject* not_implemented(CallFrame* current, IshtarObject** args)
        => throw new NotImplementedException();

    public static void InitTable(ForeignFunctionInterface ffi)
    {
        ffi.Add("file_read_all_text([std]::std::String) -> [std]::std::String", ffi.AsNative(&not_implemented));
        ffi.Add("file_write_all_text([std]::std::String,[std]::std::String) -> [std]::std::Void",
            ffi.AsNative(&not_implemented));
        ffi.Add("file_file_create([std]::std::String) -> [std]::std::StreamWriter", ffi.AsNative(&not_implemented));
    }
}
