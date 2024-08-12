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
}

