namespace ishtar;

public static partial class KnowTypes
{
    public static partial class WrappedTypes
    {
        public unsafe class S_NativeHandle : RuntimeLayerObject<S_NativeHandle>
        {
            protected override RuntimeIshtarClass Class =>  VeinLang.Native.NativeHandle(_frame);

            public S_NativeHandle(IshtarObject* @object, CallFrame frame) : base(@object, frame) { }

            public IntPtr Handle
            {
                get => IshtarMarshal.ToDotnetPointer((IshtarObject*)_obj->vtable[GetFieldOffset("_handle")], _frame);
                set => _obj->vtable[GetFieldOffset("_handle")] = _frame.GetGC().ToIshtarObject(value, _frame);
            }
        }
    }
}
