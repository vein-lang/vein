namespace ishtar
{
    public abstract unsafe class RuntimeLayerObject<T> where T : RuntimeLayerObject<T>
    {
        protected readonly IshtarObject* _obj;
        protected readonly CallFrame _frame;

        protected RuntimeLayerObject(IshtarObject* obj, CallFrame frame)
        {
            _obj = obj;
            _frame = frame;
            VirtualMachine.Assert(obj->decodeClass().runtime_token == Class.runtime_token, WNE.TYPE_MISMATCH,
                "Mismatch type when trying create layered object.", frame);
        }

        protected abstract RuntimeIshtarClass Class { get; }

        private Dictionary<string, uint> offset_field_table = new();
        private Dictionary<string, uint> offset_method_table = new(); // maybe it not needed
        protected uint GetFieldOffset(string name)
        {
            if (offset_field_table.ContainsKey(name))
                return offset_field_table[name];

            VirtualMachine.Assert(Class.Field[name] is not null, WNE.MISSING_FIELD, $"Field '{name}' is not found in '{Class.Name}' class. [Layered object]");

            return offset_field_table[name] = Class.Field[name].vtable_offset;
        }
        protected uint GetMethodOffset(string name)
        {
            if (offset_method_table.ContainsKey(name))
                return offset_method_table[name];

            VirtualMachine.Assert(Class.Method[name] is not null, WNE.MISSING_METHOD, $"Method '{name}' is not found in '{Class.Name}' class. [Layered object]");

            return offset_method_table[name] = Class.Method[name].vtable_offset;
        }

        protected IshtarObject* CallMethodAndGetObject(string methodName)
        {
            var method = Class.Method[methodName];
            var pointer = _obj->vtable[method.vtable_offset];
            var result = ExecuteMethod(pointer);
            return (IshtarObject*)result->data.p;
        }


        protected stackval* ExecuteMethod(void* method)
        {
            var runtime_method = IshtarUnsafe.AsRef<RuntimeIshtarMethod>(method);
            var callFrame = new CallFrame(_frame.vm)
            {
                parent = _frame,
                level = _frame.level + 1,
                method = runtime_method
            };

            _frame.vm.exec_method(callFrame);

            if (callFrame.exception is not null)
                _frame.exception = callFrame.exception;

            return callFrame.returnValue.Ref;
        }
    }
}
