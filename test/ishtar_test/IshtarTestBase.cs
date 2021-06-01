namespace ishtar_test
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using ishtar;
    using mana.backend.ishtar.light;
    using mana.extensions;
    using mana.ishtar.emit;
    using mana.runtime;
    using Xunit;

    public class IshtarTestContext : IDisposable
    {
        private readonly string _testCase;
        private readonly ManaModuleBuilder _module;
        private short _stack_size = 48;
        private Action<RuntimeIshtarClass, dynamic> _classCtor;
        private readonly dynamic _context;
        private ClassBuilder @class;
        private RuntimeIshtarClass runtime_class;
        private static readonly object guarder = new ();
        internal string UID { get; }

        public IshtarTestContext WithStackSize(short size = 48)
        {
            _stack_size = size;
            return this;
        }

        public IshtarTestContext OnClassBuild(Action<RuntimeIshtarClass, dynamic> action)
        {
            _classCtor = action;
            return this;
        }

        private bool isBaked { get; set; }

        public void Bake()
        {
            if (isBaked) return;
            @class = _module.DefineClass($"global::test/testClass_{_testCase}_{UID}");
            runtime_class = new RuntimeIshtarClass(@class.FullName, @class.Parent, _module);
            _module.InternTypeName(runtime_class.FullName);
            _module.class_table.Remove(@class);
            _module.class_table.Add(runtime_class);
            _classCtor?.Invoke(runtime_class, _context);
            isBaked = true;
        }

        public unsafe CallFrame Execute(Action<ILGenerator, dynamic> ctor)
        {
            lock (guarder)
            {
                Bake();
                var _method = @class.DefineMethod($"master_{_testCase}_{UID}", MethodFlags.Public | MethodFlags.Static,
                    ManaTypeCode.TYPE_OBJECT.AsClass());

                var gen = _method.GetGenerator();
                ctor(gen, _context);
                var code = gen.BakeByteArray();
                
                var entry_point = new RuntimeIshtarMethod($"master_{_testCase}_{UID}", MethodFlags.Public | MethodFlags.Static,
                    ManaTypeCode.TYPE_OBJECT.AsClass()) { Owner = runtime_class };

                entry_point.Owner.Owner = _module;
                runtime_class.init_vtable();
                return RunIt(entry_point, code, _stack_size);
            }
        }

        private static unsafe CallFrame RunIt(RuntimeIshtarMethod entry, byte[] code, short stack_size)
        {
            RuntimeModuleReader.ConstructIL(entry, code, stack_size);
            var args_ = stackalloc stackval[1];
            var frame = new CallFrame
            {
                args = args_, 
                method = entry, 
                level = 0
            };
            VM.exec_method(frame);
            return frame;
        }

        public IshtarTestContext(string testCase, ManaModuleBuilder module)
        {
            _testCase = testCase;
            _module = module;
            UID = Guid.NewGuid().ToString().Where(char.IsLetter).Join();
            _context = new ExpandoObject();
            VM.watcher = new TestWatchDog();
        }
        public void Dispose()
        {
            StringStorage.storage_l.Clear();
            StringStorage.storage_r.Clear();
            VM.CurrentException = null;
        }
    }

    public abstract class IshtarTestBase : IDisposable
    {
        private static ManaModuleBuilder _module_instance;
        private static ManaModule _corlib;
        private static ManaModuleBuilder _module
        {
            get
            {
                if (_module_instance is null)
                    return _module_instance = new ManaModuleBuilder("tst") { Deps = new List<ManaModule> { _corlib }};
                return _module_instance;
            }
        }

        private static volatile bool isInited = false;

        protected IshtarTestBase()
        {
            lock (guarder)
            {
                if (!isInited)
                {
                    IshtarCore.INIT();
                    IshtarGC.INIT();
                    FFI.INIT();
                    _corlib = LoadCorLib();
                    IshtarCore.INIT_ADDITIONAL_MAPPING();
                    foreach (var @class in ManaCore.All.OfType<RuntimeIshtarClass>()) 
                        @class.init_vtable();
                    // ReSharper disable once VirtualMemberCallInConstructor
                    StartUp();
                    isInited = true;
                }
            }
        }

        private static ManaModule LoadCorLib()
        {
            var resolver = new AssemblyResolver();
            resolver.AddSearchPath(new DirectoryInfo("./"));
            return resolver.ResolveDep("corlib", new Version(1, 0, 0), new List<ManaModule>());
        }

        private static void VMOnValidateLastErrorEvent(NativeException obj) =>
            Assert.False(true, $"native exception was thrown.\n\t" +
                               $"[{obj.code}]\n\t" +
                               $"'{obj.msg}'");

        private static readonly object guarder = new ();


        protected IshtarTestContext CreateContext([CallerMemberName] string caller = "<unnamed>")
            => new (caller, _module);
        void IDisposable.Dispose()
        {
            Shutdown();
        }

        protected void Validate()
        {
        }
        
        protected virtual void StartUp() {}
        protected virtual void Shutdown() {}
    }
}