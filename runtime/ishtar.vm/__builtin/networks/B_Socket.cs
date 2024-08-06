namespace ishtar.vm.__builtin.networks;

public unsafe class B_Socket
{
    public static IshtarObject* not_implemented(CallFrame* current, IshtarObject** args)
        => throw new NotImplementedException();


    public static void InitTable(ForeignFunctionInterface ffi)
    {
        ffi.Add("socket_tcp_init_handle() -> [std]::std::Raw",
            ffi.AsNative(&not_implemented));
        ffi.Add("socket_accept_handle([std]::std::Raw,[std]::std::Raw) -> [std]::std::Int32",
            ffi.AsNative(&not_implemented));
        ffi.Add("socket_read_start([std]::std::Raw,[std]::std::rawOnReadStart) -> [std]::std::Int32",
            ffi.AsNative(&not_implemented));
        ffi.Add("socket_create_stream_reader([std]::std::Int32,[std]::std::Raw) -> [std]::std::StreamReader",
            ffi.AsNative(&not_implemented));
        ffi.Add("socket_create_network_stream_writer([std]::std::Raw) -> [std]::std::StreamWriter",
            ffi.AsNative(&not_implemented));
        ffi.Add("socket_init_server_handle() -> [std]::std::Raw",
            ffi.AsNative(&not_implemented));
        ffi.Add("socket_tcp_bind_handle([std]::std::Raw,[std]::std::IpEndpoint) -> [std]::std::Void",
            ffi.AsNative(&not_implemented));
        ffi.Add("socket_tcp_bind_handle([std]::std::Raw,[std]::std::Int32,[std]::std::rawOnServerConnection) -> [std]::std::Void",
            ffi.AsNative(&not_implemented));
    }
}
