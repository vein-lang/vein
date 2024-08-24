namespace ishtar;

public readonly unsafe struct Vein_Ipv4Addr(IshtarObject* o)
{
    public readonly IshtarObject* Object = o;

    public byte first
    {
        get => ((IshtarObject*)Object->vtable[Object->clazz->Field["first"]->vtable_offset])->GetUInt8();
        set => ((IshtarObject*)Object->vtable[Object->clazz->Field["first"]->vtable_offset])->SetUInt8(value);
    }

    public byte second
    {
        get => ((IshtarObject*)Object->vtable[Object->clazz->Field["second"]->vtable_offset])->GetUInt8();
        set => ((IshtarObject*)Object->vtable[Object->clazz->Field["second"]->vtable_offset])->SetUInt8(value);
    }
    public byte third
    {
        get => ((IshtarObject*)Object->vtable[Object->clazz->Field["third"]->vtable_offset])->GetUInt8();
        set => ((IshtarObject*)Object->vtable[Object->clazz->Field["third"]->vtable_offset])->SetUInt8(value);
    }
    public byte fourth
    {
        get => ((IshtarObject*)Object->vtable[Object->clazz->Field["fourth"]->vtable_offset])->GetUInt8();
        set => ((IshtarObject*)Object->vtable[Object->clazz->Field["fourth"]->vtable_offset])->SetUInt8(value);
    }

    public static RuntimeIshtarClass* GetType(CallFrame* current)
    {
        var vault = current->vm->Vault;
        var DomainDetails_name = vault.GlobalFindTypeName("[std]::std::Ipv4Addr");
        return vault.GlobalFindType(DomainDetails_name, true, true);
    }
}

public readonly unsafe struct Vein_Socket(IshtarObject* o)
{
    public readonly IshtarObject* Object = o;

    public int family
    {
        get => ((IshtarObject*)Object->vtable[Object->clazz->Field["_family"]->vtable_offset])->GetInt32();
        set => ((IshtarObject*)Object->vtable[Object->clazz->Field["_family"]->vtable_offset])->SetInt32(value);
    }

    public int streamKind
    {
        get => ((IshtarObject*)Object->vtable[Object->clazz->Field["_streamKind"]->vtable_offset])->GetInt32();
        set => ((IshtarObject*)Object->vtable[Object->clazz->Field["_streamKind"]->vtable_offset])->SetInt32(value);
    }
    public int protocol
    {
        get => ((IshtarObject*)Object->vtable[Object->clazz->Field["_protocol"]->vtable_offset])->GetInt32();
        set => ((IshtarObject*)Object->vtable[Object->clazz->Field["_protocol"]->vtable_offset])->SetInt32(value);
    }

    public Vein_IpEndpoint addr
    {
        get => new(((IshtarObject*)Object->vtable[Object->clazz->Field["_addr"]->vtable_offset]));
        set => Object->vtable[Object->clazz->Field["_addr"]->vtable_offset] = value.Object;
    }

    public long handle
    {
        get => ((IshtarObject*)Object->vtable[Object->clazz->Field["_handle"]->vtable_offset])->GetInt64();
        set => ((IshtarObject*)Object->vtable[Object->clazz->Field["_handle"]->vtable_offset])->SetInt64(value);
    }
    public static RuntimeIshtarClass* GetType(CallFrame* current)
    {
        var vault = current->vm->Vault;
        var DomainDetails_name = vault.GlobalFindTypeName("[std]::std::Socket");
        return vault.GlobalFindType(DomainDetails_name, true, true);
    }
}

