namespace ishtar;

using static libuv.LibUV;

public readonly unsafe struct Vein_SocketHandle(IshtarObject* o)
{
    public uv_tcp_t* server_handle
    {
        get => (uv_tcp_t*)o->vtable[o->clazz->Field["server_handle"]->vtable_offset];
        set => o->vtable[o->clazz->Field["server_handle"]->vtable_offset] = value;
    }
}

