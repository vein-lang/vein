namespace ishtar;

using vein.runtime;

public unsafe class IshtarFunction
{
    private readonly IshtarObject* _o;
    private readonly CallFrame _frame;
    private RuntimeIshtarClass Class => VeinCore.FunctionClass as RuntimeIshtarClass;

    public IshtarFunction(IshtarObject* @object, CallFrame frame)
    {
        _o = @object;
        _frame = frame;
    }

    public string Name
    {
        get
        {
            var result = CallMethodAndGetObject("get_Name");
            return IshtarMarshal.ToDotnetString(result, _frame);
        }
    }


    private IshtarObject* CallMethodAndGetObject(string methodName)
    {
        var method = Class.Method[methodName];
        var pointer = _o->vtable[method.vtable_offset];
        var result = ExecuteMethod(pointer);
        return (IshtarObject*)result->data.p;
    }


    private stackval* ExecuteMethod(void* method)
    {
        var runtime_method = IshtarUnsafe.AsRef<RuntimeIshtarMethod>(method);
        var callFrame = new CallFrame()
        {
            parent = _frame,
            level = _frame.level + 1,
            method = runtime_method
        };

        VM.exec_method(callFrame);

        if (callFrame.exception is not null)
            _frame.exception = callFrame.exception;

        return callFrame.returnValue;
    }
}
