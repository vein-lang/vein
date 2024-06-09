namespace ishtar_test
{
    using ishtar;
    using ishtar.collections;
    using ishtar.emit;
    using ishtar.runtime;
    using ishtar.runtime.gc;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using vein.extensions;
    using vein.fs;
    using vein.runtime;

    public class IshtarTestModuleResolver : ModuleResolverBase
    {
        protected override void debug(string s) => Console.WriteLine(s);
    }

    public class IshtarPreparationContext(string testCase, string uid) : IDisposable
    {
        public Action<ClassBuilder, dynamic> ClassCtor;
        public ClassBuilder Class;
        public VeinModuleBuilder Module
        {
            get
            {
                _corlib ??= LoadCorLib();

                if (_module is null)
                    return _module = new VeinModuleBuilder("tst", Types) { Deps = [_corlib] };
                return _module;
            }
        }

        private VeinModule _corlib;
        private VeinModuleBuilder _module;
        private IshtarTestModuleResolver _resolver;
        private readonly dynamic _context = new ExpandoObject();

        public VeinCore Types { get; } = new();

        private VeinModule LoadCorLib()
        {
            _resolver ??= new IshtarTestModuleResolver();

            _resolver.AddSearchPath(new DirectoryInfo("./"));
            return _resolver.ResolveDep("std", new Version(0, 0), new List<VeinModule>());
        }

        public IshtarPreparationContext OnClassBuild(Action<ClassBuilder, dynamic> action)
        {
            ClassCtor = action;
            return this;
        }

        public void OnCodeBuild(Action<ILGenerator, dynamic> ctor)
        {
            Class = Module.DefineClass($"global::test/testClass_{testCase}_{uid}");
            ClassCtor?.Invoke(Class, _context);
            var _method = Class.DefineMethod($"master_{testCase}_{uid}", MethodFlags.Public | MethodFlags.Static,
                VeinTypeCode.TYPE_OBJECT.AsClass()(Types));

            var gen = _method.GetGenerator();
            ctor(gen, _context);
        }

        public void EnsureType(QualityTypeName type) => Module.InternTypeName(type);

        private readonly List<IDisposable> _disposables = new();

        private bool isDisposed;
        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            foreach (var disposable in _disposables) disposable.Dispose();
            _disposables.Clear();

        }


        public IshtarExecutionContext Compile()
        {
            var ctx = new IshtarExecutionContext(Module, testCase, uid);
            _disposables.Add(ctx);
            ctx.Compile();
            return ctx;
        }
    }

    public unsafe class IshtarExecutionContext : IDisposable
    {
        private readonly VeinModuleBuilder _veinModule;
        private readonly string _testCase;
        private readonly string _uid;
        public CallFrame* entryPointFrame;
        private RuntimeIshtarMethod* entryPointMethod;

        private readonly NativeList<RuntimeIshtarModule>* _deps;
        private RuntimeIshtarModule* _corlib;
        public VirtualMachine VM { get; }


        private static nint _gcHandle;
        public IshtarExecutionContext(VeinModuleBuilder veinModule, string testCase, string uid)
        {
            if (_gcHandle == 0 && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                _gcHandle = NativeLibrary.Load("libgc.so");
            BoehmGCLayout.Native.GC_init();

            _veinModule = veinModule;
            _testCase = testCase;
            _uid = uid;
            _deps = IshtarGC.AllocateList<RuntimeIshtarModule>();
            VM = VirtualMachine.Create($"test-app-{uid}-{testCase}");

            if (VM.watcher is DefaultWatchDog)
                VM.watcher = new TestWatchDog();
            _corlib = LoadCorLib();
        }

        public void Compile()
        {
            var resolver = VM.Vault.GetResolver();

            var runtimeModule = resolver.Resolve(new IshtarAssembly(_veinModule));
            entryPointMethod = runtimeModule->GetSpecialEntryPoint($"master_{_testCase}_{_uid}()");

            var args_ = stackalloc stackval[1];

            var frame = CallFrame.Create(entryPointMethod, null);
            frame->args = args_;
            entryPointFrame = frame;
        }


        public IshtarExecutionContext Execute()
        {
            VM.exec_method(entryPointFrame);
            return this;
        }

        public CallFrame* Validate()
        {
            if (entryPointFrame->exception.value == null)
                return entryPointFrame;

            var ex = entryPointFrame->exception.value;
            var clazz = ex->clazz;
            Assert.Fail($"Fault has throw. [{clazz->Name}]");

            return entryPointFrame;
        }

        private RuntimeIshtarModule* LoadCorLib()
        {
            var resolver = VM.Vault.GetResolver();
            resolver.AddSearchPath(new DirectoryInfo("./"));
            return resolver.ResolveDep("std", new IshtarVersion(0, 0), _deps);
        }

        private bool isDisposed;

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            VM?.Dispose();
            IshtarGC.FreeList(_deps);
        }
    }

    public class IshtarTestBase : IDisposable
    {
        internal string UID { get; } = Guid.NewGuid().ToString("N");

        private readonly IList<IDisposable> _disposables = new List<IDisposable>();

        public IshtarPreparationContext CreateScope([CallerMemberName] string caller = "<unnamed>")
        {
            var ctx = new IshtarPreparationContext(caller, UID);
            _disposables.Add(ctx);
            return ctx;
        }
        public IshtarExecutionContext CreateEmptyRuntime([CallerMemberName] string caller = "<unnamed>")
        {
            var ctx = new IshtarPreparationContext(caller, UID);
            _disposables.Add(ctx);
            return ctx.Compile();
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables) disposable.Dispose();
            _disposables.Clear();
        }
    }
}
