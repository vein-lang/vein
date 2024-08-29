namespace ishtar.__builtin.networks;

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

public unsafe class B_Socket
{
    private static readonly ConcurrentDictionary<long, Socket> sockets = new();
    private static long _refId;

    private static IshtarObject* socket_bind(CallFrame* current, IshtarObject** args)
    {
        var socket = new Vein_Socket(args[0]);
        
        var id = Interlocked.Add(ref _refId, 1);
        socket.handle = id;

        var family = (AddressFamily)socket.family;
        var socType = (SocketType)socket.streamKind;
        var protocol = (ProtocolType)socket.protocol;

        var s = new Socket(family,socType,
            protocol);
        sockets.TryAdd(id, s);

        try
        {
            s.Bind(new IPEndPoint(new IPAddress([
                    socket.addr.address.first,
                    socket.addr.address.second,
                    socket.addr.address.third,
                    socket.addr.address.fourth
                ]),
                socket.addr.port));
        }
        catch (SocketException e)
        {
            current->ThrowException(KnowTypes.SocketFault(current), $"sock err {e.ErrorCode} {e.SocketErrorCode.ToString().ToLowerInvariant()}");
            return null;
        }
        catch (Exception e)
        {
            current->vm->FastFail(WNE.STATE_CORRUPT, $"{e.GetType().Name.ToLowerInvariant()}_t", current);
        }

        return null;
    }

    private static IshtarObject* socket_connect(CallFrame* current, IshtarObject** args)
    {
        var socket = new Vein_Socket(args[0]);
        
        var id = Interlocked.Add(ref _refId, 1);
        socket.handle = id;

        var family = (AddressFamily)socket.family;
        var socType = (SocketType)socket.streamKind;
        var protocol = (ProtocolType)socket.protocol;


        var s = new Socket(family,socType,
            protocol);
        sockets.TryAdd(id, s);

        try
        {
            s.Connect(new IPEndPoint(new IPAddress([
                    socket.addr.address.first,
                    socket.addr.address.second,
                    socket.addr.address.third,
                    socket.addr.address.fourth
                ]),
                socket.addr.port));
        }
        catch (SocketException e)
        {
            current->ThrowException(KnowTypes.SocketFault(current), $"sock err {e.ErrorCode} {e.SocketErrorCode.ToString().ToLowerInvariant()}");
            return null;
        }
        catch (Exception e)
        {
            current->vm->FastFail(WNE.STATE_CORRUPT, $"{e.GetType().Name.ToLowerInvariant()}_t", current);
        }

        return null;
    }


    private static IshtarObject* socket_listen(CallFrame* current, IshtarObject** args)
    {
        var socket = new Vein_Socket(args[0]);
        var mttu = IshtarMarshal.ToDotnetInt32(args[1], current);
        sockets.TryGetValue(socket.handle, out var s);

        if (s is null)
        {
            current->vm->FastFail(WNE.STATE_CORRUPT, $"socket corrupt", current);
            return null;
        }

        try
        {
            s.Listen(mttu);
        }
        catch (SocketException e)
        {
            current->ThrowException(KnowTypes.SocketFault(current), $"sock err {e.ErrorCode} {e.SocketErrorCode.ToString().ToLowerInvariant()}");
            return null;
        }
        catch (Exception e)
        {
            current->vm->FastFail(WNE.STATE_CORRUPT, $"{e.GetType().Name.ToLowerInvariant()}_t", current);
        }

        return null;
    }

    private static IshtarObject* socket_accept(CallFrame* current, IshtarObject** args)
    {
        var socket = new Vein_Socket(args[0]);
        var client = new Vein_Socket(args[1]);
        sockets.TryGetValue(socket.handle, out var s);

        if (s is null)
        {
            current->vm->FastFail(WNE.STATE_CORRUPT, $"socket corrupt", current);
            return null;
        }

        try
        {
            var c = s.Accept();
            var id = Interlocked.Add(ref _refId, 1);
            client.handle = id;
            sockets.TryAdd(id, c);
        }
        catch (Exception e)
        {
            current->vm->FastFail(WNE.STATE_CORRUPT, $"{e.GetType().Name.ToLowerInvariant()}_t", current);
        }

        return null;
    }

    private static IshtarObject* socket_shutdown(CallFrame* current, IshtarObject** args)
    {
        var socket = new Vein_Socket(args[0]);
        var flag = IshtarMarshal.ToDotnetInt32(args[1], current);
        sockets.TryRemove(socket.handle, out var s);

        if (s is null)
        {
            current->vm->FastFail(WNE.STATE_CORRUPT, $"socket corrupt", current);
            return null;
        }

        try
        {
            s.Shutdown((SocketShutdown)flag);
        }
        catch (Exception e)
        {
            current->vm->FastFail(WNE.STATE_CORRUPT, $"{e.GetType().Name.ToLowerInvariant()}_t", current);
        }

        return null;
    }

    private static IshtarObject* socket_write(CallFrame* current, IshtarObject** args)
    {
        var socket = new Vein_Socket(args[0]);
        var memory = new Vein_Span_u8(args[1]);
        var flags = IshtarMarshal.ToDotnetInt32(args[2], current);

        sockets.TryGetValue(socket.handle, out var s);

        if (s is null)
        {
            current->vm->FastFail(WNE.STATE_CORRUPT, $"socket corrupt", current);
            return null;
        }

        try
        {
            Span<byte> buffer = stackalloc byte[memory._length];
            Span<byte> fromBuffer = new Span<byte>(memory._ptr, memory._length);

            fromBuffer.CopyTo(buffer);

            s.Send(buffer, (SocketFlags)flags, out var sockErr);

            if (sockErr != SocketError.Success)
            {
                current->ThrowException(KnowTypes.SocketFault(current), $"sock err {sockErr.ToString().ToLowerInvariant()}");
                return null;
            }

            return current->vm->gc->ToIshtarObject(s.Send(buffer), current);
        }
        catch (Exception e)
        {
            current->vm->FastFail(WNE.STATE_CORRUPT, $"{e.GetType().Name.ToLowerInvariant()}_t", current);
        }

        return current->vm->gc->ToIshtarObject(0, current);
    }

    private static IshtarObject* socket_read(CallFrame* current, IshtarObject** args)
    {
        var socket = new Vein_Socket(args[0]);
        var memory = new Vein_Span_u8(args[1]);
        var flags = IshtarMarshal.ToDotnetInt32(args[2], current);

        sockets.TryGetValue(socket.handle, out var s);

        if (s is null)
        {
            current->vm->FastFail(WNE.STATE_CORRUPT, $"socket corrupt", current);
            return null;
        }

        try
        {
            Span<byte> buffer = stackalloc byte[memory._length];
            Span<byte> fromBuffer = new Span<byte>(memory._ptr, memory._length);
            var result = s.Receive(buffer, (SocketFlags)flags, out var sockErr);

            if (sockErr != SocketError.Success)
            {
                current->ThrowException(KnowTypes.SocketFault(current), $"sock err {sockErr.ToString().ToLowerInvariant()}");
                return null;
            }
            

            buffer.CopyTo(fromBuffer);

            return current->vm->gc->ToIshtarObject(result, current);
        }
        catch (Exception e)
        {
            current->vm->FastFail(WNE.STATE_CORRUPT, $"{e.GetType().Name.ToLowerInvariant()}_t", current);
        }

        return current->vm->gc->ToIshtarObject(0, current);
    }

    public static void InitTable(ForeignFunctionInterface ffi)
    {
        ffi.Add("socket_bind([std]::std::Socket) -> [std]::std::Void",
            ffi.AsNative(&socket_bind));
        ffi.Add("socket_listen([std]::std::Socket,[std]::std::Int32) -> [std]::std::Void",
            ffi.AsNative(&socket_listen));
        ffi.Add("socket_accept([std]::std::Socket,[std]::std::Socket) -> [std]::std::Void",
            ffi.AsNative(&socket_accept));
        ffi.Add("socket_write([std]::std::Socket,[std]::std::Span<Byte>,[std]::std::Int32) -> [std]::std::Int32",
            ffi.AsNative(&socket_write));
        ffi.Add("socket_read([std]::std::Socket,[std]::std::Span<Byte>,[std]::std::Int32) -> [std]::std::Int32",
            ffi.AsNative(&socket_read));
        ffi.Add("socket_shutdown([std]::std::Socket,[std]::std::Int32) -> [std]::std::Void",
            ffi.AsNative(&socket_shutdown));
        ffi.Add("socket_connect([std]::std::Socket) -> [std]::std::Void",
            ffi.AsNative(&socket_connect));
    }
}

