namespace ishtar;

[ExcludeFromCodeCoverage]
public static unsafe class NativeExports
{
    public static void VM_INIT()
    {
        var vm = VirtualMachine.Create("app");
    }

    public static stackval* VM_EXECUTE_METHOD(VirtualMachine vm, Types.FrameRef* frame)
    {
        var vault = vm.Vault;
        var type = vault.GlobalFindType(*frame->runtime_token);

        if (type is null)
            return null;

        if (type.Methods.Count >= frame->index)
            return null;

        var method = type.Methods[frame->index] as RuntimeIshtarMethod;

        if (method is not { } or { IsExtern: true } or { IsStatic: false })
            return null;

        

        var callframe = new CallFrame(vm)
        {
            args = frame->args,
            level = 0,
            method = method
        };

        vm.exec_method(callframe);

        return callframe.returnValue.Ref;
    }

    public static class Types
    {
        public struct FrameRef
        {
            public stackval* args;
            public RuntimeTokenRef* runtime_token;
            public int index;
        }

        public struct RuntimeTokenRef
        {
            public ushort ModuleID;
            public ushort ClassID;


            public static implicit operator RuntimeTokenRef(RuntimeToken tok)
                => new() { ClassID = tok.ClassID, ModuleID = tok.ModuleID };
            public static implicit operator RuntimeToken(RuntimeTokenRef tok)
                => new(tok.ModuleID, tok.ClassID);
        }
    }
}
