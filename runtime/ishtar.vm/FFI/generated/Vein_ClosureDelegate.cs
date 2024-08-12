namespace ishtar;

public readonly unsafe struct Vein_ClosureDelegate(IshtarObject* o)
{
    public readonly IshtarObject* Object = o;

    public IshtarObject* Scope => ((IshtarObject*)Object->vtable[Object->clazz->Field["_scope"]->vtable_offset]);

    public rawval* Function => ((rawval*)Object->vtable[Object->clazz->Field["_fn"]->vtable_offset]);

    public bool IsVolatile => Scope is null;
}
