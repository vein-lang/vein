namespace ishtar
{
    using System.Linq;
    using System.Runtime.InteropServices;
    using vein.runtime;

    public static unsafe class NativeExports
    {
        [UnmanagedCallersOnly]
        public static void VM_INIT()
        {
            IshtarCore.INIT();
            INIT_VTABLES();
            IshtarGC.INIT();
            FFI.INIT();

            AppVault.CurrentVault = new AppVault("app");
        }

        [UnmanagedCallersOnly]
        public static void VM_VALIDATE_LAST_ERROR() => VM.ValidateLastError();

        [UnmanagedCallersOnly]
        public static stackval* VM_EXECUTE_METHOD(Types.FrameRef* frame)
        {
            var type = AppVault.CurrentVault.GlobalFindType(*frame->runtime_token);

            if (type is null)
                return null;

            if (type.Methods.Count >= frame->index)
                return null;

            var method = type.Methods[frame->index] as RuntimeIshtarMethod;

            if (method is not { } or { IsExtern: true } or { IsStatic: false })
                return null;


            var callframe = new CallFrame()
            {
                args = frame->args,
                level = 0,
                method = method
            };

            VM.exec_method(callframe);

            return callframe.returnValue;
        }

        private static void INIT_VTABLES()
        {
            foreach (var @class in VeinCore.All.OfType<RuntimeIshtarClass>())
                @class.init_vtable();
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
}
