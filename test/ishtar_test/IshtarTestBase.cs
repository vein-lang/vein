namespace ishtar_test
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using ishtar;
    using vein.extensions;
    using vein.fs;
    using ishtar.emit;
    using vein.runtime;
    using Xunit;
    using Assert = NUnit.Framework.Assert;

    public static class IshtarRuntimeModuleEx
    {
        public static RuntimeIshtarMethod GetEntryPoint(this RuntimeIshtarModule module, string name)
        {
            foreach (var method in module.class_table.SelectMany(x => x.Methods))
            {
                if (!method.IsStatic)
                    continue;
                if (method.Name == $"{name}()")
                    return (RuntimeIshtarMethod)method;
            }

            return null;
        }
    }

    public class IshtarTestContext : IDisposable
    {
        private readonly string _testCase;
        private readonly VeinModuleBuilder _module;
        private Action<ClassBuilder, dynamic> _classCtor;
        private readonly dynamic _context;
        private ClassBuilder @class;
        internal string UID { get; }
        internal CallFrame entryPointFrame;

        public VirtualMachine vm { get; }

        public IshtarTestContext OnClassBuild(Action<ClassBuilder, dynamic> action)
        {
            _classCtor = action;
            return this;
        }

        private void OnCodeBuild(Action<ILGenerator, dynamic> ctor)
        {
            @class = _module.DefineClass($"global::test/testClass_{_testCase}_{UID}");
            _classCtor?.Invoke(@class, _context);
            var _method = @class.DefineMethod($"master_{_testCase}_{UID}", MethodFlags.Public | MethodFlags.Static,
                VeinTypeCode.TYPE_OBJECT.AsClass()(vm.Types));

            var gen = _method.GetGenerator();
            ctor(gen, _context);
        }


        public unsafe CallFrame Execute(Action<ILGenerator, dynamic> ctor)
        {
            if (vm.watcher is DefaultWatchDog)
                vm.watcher = new TestWatchDog();
            var resolver = vm.Vault.GetResolver();

            OnCodeBuild(ctor);

            var runtimeModule = resolver.Resolve(new IshtarAssembly(_module));

            var entry_point = runtimeModule.GetEntryPoint($"master_{_testCase}_{UID}");
            foreach (var c in vm.Types.All.OfType<RuntimeIshtarClass>())
                c.init_vtable(vm);
            foreach (var c in runtimeModule.class_table.OfType<RuntimeIshtarClass>())
                c.init_vtable(vm);
            return entryPointFrame = RunIt(entry_point);
        }

        private unsafe CallFrame RunIt(RuntimeIshtarMethod entry)
        {
            if (vm.watcher is DefaultWatchDog)
                vm.watcher = new TestWatchDog();
            var args_ = stackalloc stackval[1];
            var frame = new CallFrame(vm)
            {
                args = args_,
                method = entry,
                level = 0
            };
            vm.exec_method(frame);
            return entryPointFrame = frame;
        }

        public IshtarTestContext(string testCase, VeinModuleBuilder module, VirtualMachine vm)
        {
            _testCase = testCase;
            _module = module;
            UID = Guid.NewGuid().ToString().Where(char.IsLetter).Join();
            this.vm = vm;
            _context = new ExpandoObject();
            vm.watcher = new TestWatchDog();
        }

        private bool isDisposed;

        public void Dispose()
        {
            if (isDisposed) return;

            entryPointFrame?.returnValue.Dispose();
            StringStorage.storage_l.Clear();
            StringStorage.storage_r.Clear();
            vm.CurrentException = null;

            isDisposed = true;
        }

        public void EnsureType(QualityTypeName type) => _module.InternTypeName(type);
    }

    public abstract class IshtarTestBase : IDisposable
    {
        public class PredefinedTypes(VirtualMachine _vm)
        {
            public RuntimeIshtarClass VOID => VeinTypeCode.TYPE_VOID.AsRuntimeClass(_vm.Types);
            public RuntimeIshtarClass OBJECT => VeinTypeCode.TYPE_OBJECT.AsRuntimeClass(_vm.Types);
            public RuntimeIshtarClass STRING => VeinTypeCode.TYPE_STRING.AsRuntimeClass(_vm.Types);
        }



        private VeinModuleBuilder _module_instance;
        private VeinModule _corlib;
        protected VeinModuleBuilder _module
        {
            get
            {
                if (_module_instance is null)
                    return _module_instance = new VeinModuleBuilder("tst", _vm.Types) { Deps = new List<VeinModule> { _corlib } };
                return _module_instance;
            }
        }
        private readonly VirtualMachine _vm;

        public VirtualMachine GetVM() => _vm;
        public IshtarCore Types => _vm.Types;
        public IshtarGC GC => _vm.GC;
        public PredefinedTypes T;

        protected IshtarTestBase()
        {
            _vm = VirtualMachine.Create("test-app");
            T = new PredefinedTypes(_vm);
            if (_vm.watcher is DefaultWatchDog)
                _vm.watcher = new TestWatchDog();
            _corlib = LoadCorLib();
            foreach (var @class in _vm.Types.All.OfType<RuntimeIshtarClass>())
                @class.init_vtable(_vm);
        }

        private VeinModule LoadCorLib()
        {
            var resolver = _vm.Vault.GetResolver();
            resolver.AddSearchPath(new DirectoryInfo("./"));
            return resolver.ResolveDep("std", new Version(0, 0, 0), new List<VeinModule>());
        }

        protected IshtarTestContext CreateContext([CallerMemberName] string caller = "<unnamed>")
        {
            var ctx = new IshtarTestContext(caller, _module, _vm);
            toDisposables.Add(ctx);
            return ctx;
        }

        void IDisposable.Dispose() => Shutdown();

        protected void Validate()
        {
        }

        protected unsafe void Validate(CallFrame frame)
        {
            if (frame.exception is not null)
            {
                var exception = frame.exception.value;
                var clazz = exception->decodeClass();
                Assert.Fail($"Fault has throw. [{clazz.Name}]");
            }
        }

        private List<IDisposable> toDisposables = new List<IDisposable>();

        protected virtual void StartUp() { }

        protected virtual void Shutdown()
        {
            foreach (var disposable in toDisposables) disposable.Dispose();
            _vm.Dispose();
        }
    }
}
