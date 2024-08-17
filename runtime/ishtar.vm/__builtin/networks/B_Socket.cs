namespace ishtar.__builtin.networks;

using runtime.gc;
using System;
using System.Collections.Concurrent;
using System.IO;
using vein.runtime;
using static ForeignFunctionInterface;
using static libuv.LibUV;
using static libuv.UV_ERR;
using static VirtualMachine;

public unsafe struct ishtar_client_tcp_handle
{
    public VirtualMachine* vm;
    public uv_tcp_t* socketHandle;
    public IshtarObject* clientHandleObj;
    public int size_buffer;
    public byte* buffer;
}

public unsafe struct ishtar_tcp_handle
{
    public VirtualMachine* vm;
    public IshtarObject* OnConnection_Closure;
    public Vein_ClosureDelegate OnConnection_Delegate => new(OnConnection_Closure);
    public uv_tcp_t* socketHandle;

    public static void OnConnection(uv_tcp_t* server, int status)
    {
        var ishtarHandler = (ishtar_tcp_handle*)server->data;
        var vm = ishtarHandler->vm;
        var gc = vm->gc;
        var loop = server->loop;
        var frame = CallFrame.Create(ishtarHandler->OnConnection_Delegate.Function->data.m);
        var closureScopeObj = ishtarHandler->OnConnection_Delegate.Scope;

        frame->args = gc->AllocateStack(frame, frame->method->ArgLength);

        Assert(!frame->method->IsStatic,
            closureScopeObj != null,
            WNE.GC_MOVED_UNMOVABLE_MEMORY, "closure scope has deleted or moved", frame);

        frame->args[0] = new stackval()
        {
            type = VeinTypeCode.TYPE_CLASS,
            data =
            {
                p = (nint)ishtarHandler->OnConnection_Delegate.Scope
            }
        };
        frame->args[2] = new stackval()
        {
            type = VeinTypeCode.TYPE_I4,
            data = { i = status }
        };
        if (status != 0)
        {
            frame->args[1] = new stackval()
            {
                type = VeinTypeCode.TYPE_NULL,
                data = { p = 0 }
            };
            ishtarHandler->vm->task_scheduler->execute_method(frame);
            return;
        }

        uv_tcp_t* client = gc->AllocateUVStruct<uv_tcp_t>(frame);
        var err = uv_tcp_init(loop, client);

        if (err != OK)
        {
            frame->ThrowException(KnowTypes.SocketFault(frame), $"libuv_err {err}");
            return;
        }

        var handle = gc->AllocateSystemStruct<ishtar_client_tcp_handle>(frame);

        handle->socketHandle = client;
        handle->vm = vm;
        client->data = handle;

        var vault = AppVault.GetVault(vm);
        var socketClientName = vault.GlobalFindTypeName("[std]::std::ClientHandle");
        var socketClientHandle = vault.GlobalFindType(socketClientName, true, true);
        var clientHandle = gc->AllocObject(socketClientHandle, frame);
        handle->clientHandleObj = clientHandle;


        frame->args[1] = new stackval()
        {
            type = VeinTypeCode.TYPE_CLASS,
            data = { p = (nint)clientHandle }
        };

        var proxy = new Vein_ClientSocketHandle(clientHandle);

        proxy.client_handle = client;

        ishtarHandler->vm->exec_method(frame);
    }
}

public unsafe class B_Socket
{
    private static IshtarObject* not_implemented(CallFrame* current, IshtarObject** args)
        => throw new NotSupportedException();

    
    private static IshtarObject* socket_receive(CallFrame* current, IshtarObject** args)
    {
        var clientHandle = new Vein_ClientSocketHandle(args[0]);
        var buffer = new Vein_Span_u8(args[1]);

        clientHandle.IshtarHandle->size_buffer = (int)buffer._length;

        var result = uv_read_start(
            clientHandle.client_handle,
            &allocate_uv_buffer,
            &on_read_callback);
        current->vm->task_scheduler->runOnce();
        if (result != OK)
            throw new InvalidOperationException(); // todo

        if (clientHandle.IshtarHandle->size_buffer == 0)
        {
            if (clientHandle.IshtarHandle->buffer != null)
                IshtarGC.FreeAtomicImmortal(clientHandle.IshtarHandle->buffer);
            return current->vm->gc->ToIshtarObject(0, current);
        }
        
        buffer.CopyFrom(clientHandle.IshtarHandle->buffer, clientHandle.IshtarHandle->size_buffer);

        IshtarGC.FreeAtomicImmortal(clientHandle.IshtarHandle->buffer);

        return current->vm->gc->ToIshtarObject((int)clientHandle.IshtarHandle->size_buffer, current);
    }

    public static void on_read_callback(uv_tcp_t* stream, IntPtr nread, uv_buf_t* buffer)
    {
        var client = (ishtar_client_tcp_handle*)stream->data;

        if (nread <= 0)
        {
            client->size_buffer = 0;
            IshtarGC.FreeAtomicImmortal(buffer->basePtr);
            return;
        }


        client->buffer = (byte*)IshtarGC.AllocateAtomicImmortal((uint)client->size_buffer);

        Unsafe.CopyBlock(client->buffer, buffer->basePtr, (uint)buffer->len);

        IshtarGC.FreeAtomicImmortal(buffer->basePtr);
    }

    private static IshtarObject* socket_send(CallFrame* current, IshtarObject** args)
    {
        var clientHandle = new Vein_ClientSocketHandle(args[0]);
        var buffer = new Vein_Span_u8(args[1]);

        clientHandle.IshtarHandle->size_buffer = (int)buffer._length;
        var alloc = IshtarGC.AllocateImmortal<uv_buf_t>(null);
        *alloc = new uv_buf_t
        {
            basePtr = buffer._ptr,
            len = buffer._length
        };
        var result = uv_write(alloc,
            clientHandle.client_handle, alloc, 1, &writecallback);

        if (result != OK)
        {
            current->ThrowException(KnowTypes.SocketFault(current), $"libuv_err {result}");
            return null;
        }
        return null;
    }

    private static void writecallback(uv_tcp_t* handle, int status)
    {

    }

    private static void allocate_uv_buffer(uv_tcp_t* handle, IntPtr size, uv_buf_t* buffer)
    {
        var client = (ishtar_client_tcp_handle*)handle->data;

        buffer->basePtr = IshtarGC.AllocateAtomicImmortal((uint)client->size_buffer);
        buffer->len = client->size_buffer;
    }


    private static IshtarObject* tcp_listen_handle(CallFrame* current, IshtarObject** args)
    {
        var socketHandlerProxy = new Vein_SocketHandle(args[0]);
        var nap = args[1]->GetInt32();
        var caller = args[2];
        var closure = new Vein_ClosureDelegate(caller);

        Assert(caller != null,
            WNE.GC_PREFIRED, "[caller] tcp_listen_handle argument invalid", current);
        Assert(closure.Function != null,
            WNE.GC_PREFIRED, "[caller] tcp_listen_handle.Function argument invalid", current);
        Assert(!closure.Function->data.m->IsStatic,
            closure.Scope != null, WNE.GC_PREFIRED, "closure scope null, but method is not static", current);

        var ishtarHandle = (ishtar_tcp_handle*)socketHandlerProxy.server_handle->data;
        ishtarHandle->OnConnection_Closure = caller;

        new Thread(() =>
        {
            var err = uv_listen(socketHandlerProxy.server_handle, nap, &ishtar_tcp_handle.OnConnection);

            if (err != OK)
            {
                current->ThrowException(KnowTypes.SocketFault(current), $"libuv_err {err}");
                return;
            }
        }).Start();

        return null;
    }


    private static IshtarObject* socket_accept_handle(CallFrame* current, IshtarObject** args)
    {
        StaticTypeOf(current, args[0], VeinTypeCode.TYPE_CLASS);
        StaticTypeOf(current, args[1], VeinTypeCode.TYPE_CLASS);
        var clientHandle = new Vein_ClientSocketHandle(args[0]);
        var serverHandle = new Vein_SocketHandle(args[1]);
        var result = uv_accept(serverHandle.server_handle, clientHandle.client_handle);
        var status = current->vm->gc->ToIshtarObject((int)result, current);

        return status;
    }

    private static IshtarObject* socket_init_server_handle(CallFrame* current, IshtarObject** args)
    {
        var loop = current->vm->task_scheduler->getLoop();
        var vault = AppVault.GetVault(current->vm);
        var socketHandleName = vault.GlobalFindTypeName("[std]::std::SocketHandle");
        var socketHandle = vault.GlobalFindType(socketHandleName, true, true);
        var gc = current->GetGC();
        StaticTypeOf(current, args, VeinTypeCode.TYPE_CLASS);

        var ipEndpoint = new Vein_IpEndpoint(args[0]);

        var handle = current->GetGC()->AllocObject(socketHandle, current);
        var proxy = new Vein_SocketHandle(handle);


        var tcpHandle = gc->AllocateSystemStruct<uv_tcp_t>(current);

        var result = uv_tcp_init(loop, tcpHandle);


        var ishtarHandler = gc->AllocateSystemStruct<ishtar_tcp_handle>(current);


        ishtarHandler->vm = current->vm;
        ishtarHandler->socketHandle = tcpHandle;
        tcpHandle->data = ishtarHandler;

        proxy.server_handle = tcpHandle;

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
        ffi.Add("socket_receive([std]::std::ClientHandle,[std]::std::Object) -> [std]::std::Int32",
            ffi.AsNative(&socket_receive));
        ffi.Add("socket_send([std]::std::ClientHandle,[std]::std::Object) -> [std]::std::Void",
            ffi.AsNative(&socket_send));
        ffi.Add("socket_tcp_init_handle() -> [std]::std::Raw",
            ffi.AsNative(&not_implemented));
        ffi.Add("socket_accept_handle([std]::std::ClientHandle,[std]::std::SocketHandle) -> [std]::std::Int32",
            ffi.AsNative(&socket_accept_handle));
        ffi.Add("socket_tcp_bind_handle([std]::std::IpEndpoint) -> [std]::std::SocketHandle",
            ffi.AsNative(&socket_init_server_handle));
        ffi.Add("socket_tcp_bind_handle([std]::std::Raw,[std]::std::IpEndpoint) -> [std]::std::Void",
            ffi.AsNative(&not_implemented));
        ffi.Add("tcp_listen_handle([std]::std::SocketHandle,[std]::std::Int32,[std]::std::rawOnServerConnection) -> [std]::std::Void",
            ffi.AsNative(&tcp_listen_handle));
    }
}

