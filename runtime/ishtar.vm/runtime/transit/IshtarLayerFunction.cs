//namespace ishtar;

//public unsafe class IshtarLayerField : RuntimeLayerObject<IshtarLayerField>
//{
//    protected override RuntimeIshtarClass* Class => KnowTypes.Field(_frame);

//    public IshtarLayerField(IshtarObject* @object, CallFrame frame)
//        : base(@object, frame) { }


//    public long VTOffset
//        => IshtarMarshal.ToDotnetInt64((IshtarObject*)_obj->vtable[GetFieldOffset("_vtoffset")], _frame);
//    public string Name
//        => IshtarMarshal.ToDotnetString((IshtarObject*)_obj->vtable[GetFieldOffset("_name")], _frame);
//}

//public unsafe class IshtarLayerFunction : RuntimeLayerObject<IshtarLayerFunction>
//{
//    protected override RuntimeIshtarClass Class => KnowTypes.Function(_frame);

//    public IshtarLayerFunction(IshtarObject* @object, CallFrame frame) : base(@object, frame) { }

//    public string Name
//        => IshtarMarshal.ToDotnetString((IshtarObject*)_obj->vtable[GetFieldOffset("_name")], _frame);
//}
