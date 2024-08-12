namespace ishtar.__builtin.networks;

using runtime.gc;
using vein.runtime;
using static libuv.LibUV;
using static libuv.UV_ERR;

public unsafe struct ishtar_tcp_handle
{
    public VirtualMachine* vm;
    public IshtarObject* OnConnection_Closure;
    public Vein_ClosureDelegate OnConnection_Delegate => new(OnConnection_Closure);
    public uv_tcp_t* socketHandle;

    public static void OnConnection(uv_tcp_t* server, int status)
    {
        var ishtarHandler = (ishtar_tcp_handle*)server->data;

        var frame = CallFrame.Create(ishtarHandler->OnConnection_Delegate.Function->data.m);

        //ishtarHandler->vm->task_scheduler->execute_method();
    }
}

public unsafe class B_Socket
{
    public static IshtarObject* not_implemented(CallFrame* current, IshtarObject** args)
        => throw new NotImplementedException();


    private static IshtarObject* tcp_listen_handle(CallFrame* current, IshtarObject** args)
    {
        var socketHandlerProxy = new Vein_SocketHandle(args[0]);
        var nap = args[1]->GetInt32();
        var caller = args[2];

        var ishtarHandle = (ishtar_tcp_handle*)socketHandlerProxy.server_handle->data;
        ishtarHandle->OnConnection_Closure = caller;

        var err = uv_listen(socketHandlerProxy.server_handle, nap, &ishtar_tcp_handle.OnConnection);

        if (err != OK)
        {
            current->ThrowException(KnowTypes.SocketFault(current), $"libuv_err {err}");
            return null;
        }

        return null;
    }

    

    private static IshtarObject* socket_init_server_handle(CallFrame* current, IshtarObject** args)
    {
        var loop = current->vm->task_scheduler->getLoop();
        var vault = AppVault.GetVault(current->vm);
        var socketHandleName = vault.GlobalFindTypeName("[std]::std::SocketHandle");
        var socketHandle = vault.GlobalFindType(socketHandleName, true, true);

        ForeignFunctionInterface.StaticTypeOf(current, args, VeinTypeCode.TYPE_CLASS);

        var ipEndpoint = new Vein_IpEndpoint(args[0]);

        var handle = current->GetGC()->AllocObject(socketHandle, current);
        var proxy = new Vein_SocketHandle(handle);


        var tcpHandle = current->GetGC()->AllocateUVStruct<uv_tcp_t>(current);

        var ishtarHandler = IshtarGC.AllocateImmortal<ishtar_tcp_handle>(null);

        ishtarHandler->vm = current->vm;
        ishtarHandler->socketHandle = tcpHandle;
        tcpHandle->data = ishtarHandler;

        proxy.server_handle = tcpHandle;

        var result = uv_tcp_init(loop, tcpHandle);

        if (result != OK)
        {
            current->ThrowException(KnowTypes.SocketFault(current), $"libuv_err {result}");
            return null;
        }

        var addr = ipEndpoint.address;
        var uvAddr = new sockaddr_in
        {
            sin_family = 2,
            sin_port = htons(ipEndpoint.port),
            sin_addr = inet_pton4(BitConverter.ToUInt32([addr.fourth, addr.third, addr.second, addr.first], 0))
        };


        result = uv_tcp_bind(tcpHandle, ref uvAddr, 0);

        if (result != OK )
        {
            current->ThrowException(KnowTypes.SocketFault(current), $"libuv_err {result}");
            return null;
        }

        return handle;
    }

    static ushort htons(ushort h)
    {
        byte* bytes = (byte*)&h;
        return (ushort)((bytes[0] << 8) | bytes[1]);
    }

    static uint inet_pton4(uint addr)
    {
        byte* bytes = (byte*)&addr;
        return (uint)(bytes[0] << 24) | (uint)(bytes[1] << 16) | (uint)(bytes[2] << 8) | bytes[3];
    }

    public static void InitTable(ForeignFunctionInterface ffi)
    {
        ffi.Add("socket_tcp_init_handle() -> [std]::std::Raw",
            ffi.AsNative(&not_implemented));
        ffi.Add("socket_accept_handle([std]::std::Raw,[std]::std::SocketHandle) -> [std]::std::Int32",
            ffi.AsNative(&not_implemented));
        ffi.Add("socket_read_start([std]::std::Raw,[std]::std::rawOnReadStart) -> [std]::std::Int32",
            ffi.AsNative(&not_implemented));
        ffi.Add("socket_create_stream_reader([std]::std::Int32,[std]::std::Raw) -> [std]::std::StreamReader",
            ffi.AsNative(&not_implemented));
        ffi.Add("socket_create_network_stream_writer([std]::std::Raw) -> [std]::std::StreamWriter",
            ffi.AsNative(&not_implemented));
        ffi.Add("socket_tcp_bind_handle([std]::std::IpEndpoint) -> [std]::std::SocketHandle",
            ffi.AsNative(&socket_init_server_handle));
        ffi.Add("socket_tcp_bind_handle([std]::std::Raw,[std]::std::IpEndpoint) -> [std]::std::Void",
            ffi.AsNative(&not_implemented));
        ffi.Add("tcp_listen_handle([std]::std::SocketHandle,[std]::std::Int32,[std]::std::rawOnServerConnection) -> [std]::std::Void",
            ffi.AsNative(&tcp_listen_handle));
    }
}

