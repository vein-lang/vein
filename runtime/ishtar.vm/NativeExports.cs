namespace ishtar;

using LLVMSharp.Interop;
using vein.runtime;

[ExcludeFromCodeCoverage]
public static unsafe class NativeExports
{
    public static void VM_INIT()
    {
        var vm = VirtualMachine.Create("app");
    }

    //[UnmanagedCallersOnly]
    //public static void execute_method(CallFrame** frame)
    //{

    //}
    //[UnmanagedCallersOnly]
    //public static void create_method(MarshaledString name, MethodFlags flags, RuntimeIshtarClass* returnType, RuntimeIshtarClass** args)
    //{

    //}

    public static stackval* VM_EXECUTE_METHOD(VirtualMachine vm, Types.FrameRef* frame)
    {
        throw null;
        //var vault = vm.Vault;
        //var type = vault.GlobalFindType(*frame->runtime_token);

        //if (type is null)
        //    return null;

        //if (type->Methods->Length >= frame->index)
        //    return null;

        //var method = type->Methods->Get(frame->index);

        //if (*method is not { } or { IsExtern: true } or { IsStatic: false })
        //    return null;


        //var callframe = CallFrame.Create(method, null)

        //var callframe = new CallFrame()
        //{
        //    args = frame->args,
        //    level = 0,
        //    method = method
        //};

        //vm.exec_method(callframe);

        //return callframe.returnValue.Ref;
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
            public uint ModuleID;
            public uint ClassID;


            public static implicit operator RuntimeTokenRef(RuntimeToken tok)
                => new() { ClassID = tok.ClassID, ModuleID = tok.ModuleID };
            public static implicit operator RuntimeToken(RuntimeTokenRef tok)
                => new(tok.ModuleID, tok.ClassID);
        }
    }
}
