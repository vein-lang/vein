namespace ishtar;

using ishtar.__builtin.networks;

public readonly unsafe struct Vein_IpEndpoint(IshtarObject* o)
{
    public readonly IshtarObject* Object = o;


    public ushort port
    {
        get => ((IshtarObject*)Object->vtable[Object->clazz->Field["port"]->vtable_offset])->GetUInt16();
        set => ((IshtarObject*)Object->vtable[Object->clazz->Field["port"]->vtable_offset])->SetUInt16(value);
    }

    public Vein_Ipv4Addr address
    {
        get => new((IshtarObject*)Object->vtable[Object->clazz->Field["address"]->vtable_offset]);
        set => Object->vtable[Object->clazz->Field["address"]->vtable_offset] = value.Object;
    }
}


public readonly unsafe struct Vein_DomainDetails(IshtarObject* o)
{
    public readonly IshtarObject* Object = o;
    public Vein_Ipv4Addr address
    {
        get => new((IshtarObject*)Object->vtable[Object->clazz->Field["ipv4"]->vtable_offset]);
        set => Object->vtable[Object->clazz->Field["ipv4"]->vtable_offset] = value.Object;
    }

    public static RuntimeIshtarClass* GetType(CallFrame* current)
    {
        var vault = current->vm->Vault;
        var DomainDetails_name = vault.GlobalFindTypeName("[std]::std::DomainDetails");
        return vault.GlobalFindType(DomainDetails_name, true, true);
    }
}
