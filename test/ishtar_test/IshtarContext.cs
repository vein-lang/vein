namespace ishtar_test
{
    using System;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using ishtar;
    using mana.backend.ishtar.light;
    using mana.extensions;
    using mana.ishtar.emit;
    using mana.runtime;
    using Xunit;

    public abstract class IshtarContext : IDisposable
    {
        private static ManaModuleBuilder _module;
        private static ClassBuilder _class;
        protected IshtarContext()
        {
            if (VM.allow_shutdown_process)
            {
                VM.allow_shutdown_process = false;
                IshtarCore.INIT();
                foreach (var @class in ManaCore.All.OfType<RuntimeIshtarClass>()) 
                    @class.init_vtable();
                IshtarGC.INIT();
                FFI.INIT();
                _module = new ManaModuleBuilder("tst");
                _class = _module.DefineClass("global::test/testClass");
                // ReSharper disable once VirtualMemberCallInConstructor
                StartUp();
            }
        }

        public unsafe CallFrame Execute(Action<ILGenerator> ctor, short stack_size = 48, [CallerMemberName] string caller = "")
        {
            var guid = Guid.NewGuid().ToString().Where(char.IsLetter).Join();
            var _method = _class.DefineMethod($"master_{caller}_{guid}", MethodFlags.Public | MethodFlags.Static,
                ManaTypeCode.TYPE_VOID.AsClass());

            var gen = _method.GetGenerator();
            ctor(gen);
            var code = gen.BakeByteArray();
            var args_ = stackalloc stackval[1];
            
            var entry_point = new RuntimeIshtarMethod($"master_{caller}_{guid}", MethodFlags.Public | MethodFlags.Static,
                ManaTypeCode.TYPE_VOID.AsClass()) { Owner = _class };

            entry_point.Owner.Owner = _module;
            
            RuntimeModuleReader.ConstructIL(entry_point, code, stack_size);

            var frame = new CallFrame
            {
                args = args_, 
                method = entry_point, 
                level = 0
            };
            VM.exec_method(frame);
            return frame;
        }
        
        void IDisposable.Dispose()
        {
            Shutdown();
        }

        protected void Validate()
        {
            if (VM.VMException is not null)
                Assert.False(true, $"native exception was thrown.\n\t" +
                                   $"[{VM.VMException.code}]\n\t" +
                                   $"'{VM.VMException.msg}'");
        }
        
        protected abstract void StartUp();
        protected abstract void Shutdown();
    }
}