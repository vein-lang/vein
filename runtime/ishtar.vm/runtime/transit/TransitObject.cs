namespace ishtar
{
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using vein.runtime;

    public unsafe class TransitObject<T> where T : TransitObject<T>
    {
        public TransitObject(QualityTypeName q)
        {
            this.@class = AppVault.CurrentVault.GlobalFindType(q);
            validate();
            instance = null;
        }
        public TransitObject(RuntimeIshtarClass clazz)
        {
            this.@class = clazz;
            validate();
        }

        private void validate()
        {

        }

        private RuntimeIshtarClass @class;
        private IshtarObject* instance;
        private bool is_inited = false;
        protected IValueLayer<X> Field<X>(string name)
            => new ValueLayer<X>(this, @class.Field[name]);
        public readonly struct ValueLayer<X> : IValueLayer<X>
        {
            private readonly TransitObject<T> _obj;
            private readonly RuntimeIshtarField _field;

            public ValueLayer(TransitObject<T> obj, RuntimeIshtarField field)
            {
                _obj = obj;
                _field = field;
            }

            public X MarshaledValue
            {
                get
                {
                    if (!_obj.is_inited)
                    {
                        VM.FastFail(WNE.TYPE_LOAD, $"type is not inited.");
                        VM.ValidateLastError();
                        return default;
                    }
                    return IshtarMarshal.ToDotnet<X>((IshtarObject*)_obj.instance->vtable[_field.vtable_offset], null);
                }
                set
                {
                    if (!_obj.is_inited)
                    {
                        VM.FastFail(WNE.TYPE_LOAD, $"type is not inited.");
                        VM.ValidateLastError();
                        return;
                    }
                    _obj.instance->vtable[_field.vtable_offset] = IshtarMarshal.ToIshtarObject(value, null);
                }
            }
        }
    }


    public class X_TcpListener : TransitObject<X_TcpListener>
    {
        public X_TcpListener() : base("stdglobal::vein/lang/network/TcpListener")
        {
        }

        public bool ExclusiveAddressUse => Field<bool>("ExclusiveAddressUse").MarshaledValue;
    }
}
