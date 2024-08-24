namespace ishtar;

public readonly unsafe struct Vein_Span_u8(IshtarObject* o)
{
    /* private _ptr: raw;
       private _itemSize: u32;
       private _length: u32;
       private _isDestroyed: bool;*/
    
    public byte* _ptr
    {
        get => (byte*)o->vtable[o->clazz->Field[nameof(_ptr)]->vtable_offset];
        set => o->vtable[o->clazz->Field[nameof(_ptr)]->vtable_offset] = value;
    }

    public int _itemSize
    {
        get => ((IshtarObject*)o->vtable[o->clazz->Field["_itemSize"]->vtable_offset])->GetInt32();
        set => ((IshtarObject*)o->vtable[o->clazz->Field["_itemSize"]->vtable_offset])->SetInt32(value);
    }

    public int _length
    {
        get => ((IshtarObject*)o->vtable[o->clazz->Field["_length"]->vtable_offset])->GetInt32();
        set => ((IshtarObject*)o->vtable[o->clazz->Field["_length"]->vtable_offset])->SetInt32(value);
    }

    public bool _isDestroyed
    {
        get => ((IshtarObject*)o->vtable[o->clazz->Field["_isDestroyed"]->vtable_offset])->GetInt32() == 1;
        set => ((IshtarObject*)o->vtable[o->clazz->Field["_isDestroyed"]->vtable_offset])->SetInt32((value ? 1 : 0));
    }

    public void CopyFrom(byte* source, int size)
    {
        if (size != _length)
            throw new InvalidOperationException();
        Unsafe.CopyBlock(_ptr, source, (uint)size);
    }
}

