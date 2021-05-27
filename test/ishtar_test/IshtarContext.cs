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
        private static ClassBuilder _class_instance;
        private static ManaModuleBuilder _module_instance;
        private static ClassBuilder _class
        {
            get
            {
                if (_class_instance is null)
                    return _class_instance = _module.DefineClass("global::test/testClass");
                return _class_instance;
            }
        }
        private static ManaModuleBuilder _module
        {
            get
            {
                if (_module_instance is null)
                    return _module_instance = new ManaModuleBuilder("tst");
                return _module_instance;
            }
        }

        private static bool isInited = false;

        protected IshtarContext()
        {
            lock (guarder)
            {
                if (!isInited)
                {
                    VM.allow_shutdown_process = false;
                    IshtarCore.INIT();
                    foreach (var @class in ManaCore.All.OfType<RuntimeIshtarClass>()) 
                        @class.init_vtable();
                    IshtarGC.INIT();
                    FFI.INIT();
                    // ReSharper disable once VirtualMemberCallInConstructor
                    StartUp();
                    isInited = true;
                    VM.ValidateLastErrorEvent += VMOnValidateLastErrorEvent;
                }
            }
        }

        private static void VMOnValidateLastErrorEvent(NativeException obj) =>
            Assert.False(true, $"native exception was thrown.\n\t" +
                               $"[{obj.code}]\n\t" +
                               $"'{obj.msg}'");

        private static readonly object guarder = new ();

        public unsafe CallFrame Execute(Action<ILGenerator> ctor, short stack_size = 48, [CallerMemberName] string caller = "")
        {
            lock (guarder)
            {
                VM.VMException = null;
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
        }
        
        void IDisposable.Dispose()
        {
            VM.ValidateLastErrorEvent -= VMOnValidateLastErrorEvent;
            Shutdown();
        }

        protected void Validate()
        {
        }
        
        protected abstract void StartUp();
        protected abstract void Shutdown();
    }
}