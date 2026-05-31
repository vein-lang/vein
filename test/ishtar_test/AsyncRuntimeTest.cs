namespace ishtar_test;

using System;
using System.Collections.Generic;
using System.IO;
using ishtar;
using ishtar.io;
using ishtar.runtime;
using ishtar.runtime.gc;
using ishtar.runtime.io;
using NUnit.Framework;
using vein.runtime;
using static ishtar.libuv.LibUV;

/// <summary>
/// Tests for the async runtime: IshtarAsyncJob, SuspendedFrame capture/free,
/// AWAIT fast-path (completed job).
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.None)]
public unsafe class AsyncRuntimeTest : IshtarTestBase
{
    private static VirtualMachine* _vm;
    private static bool _initialized;
    private static bool _corlibLoaded;

    [OneTimeSetUp]
    public static void FixtureSetup()
    {
        if (_initialized) return;
        _initialized = true;

        VirtualMachine.static_init();
        var bootCfg = VirtualMachine.readBootCfg();
        var appCfg = new AppConfig(bootCfg);
        _vm = VirtualMachine.Create("async-test-vm", appCfg);

        // Load corlib into this VM once
        var resolver = _vm->Vault.GetResolver();
        resolver.AddSearchPath(new DirectoryInfo("./"));
        var deps = IshtarGC.AllocateList<RuntimeIshtarModule>(_vm);
        resolver.ResolveDep("std", new IshtarVersion(0, 0), deps);
        _corlibLoaded = true;
    }

    /// <summary>
    /// Build a VeinModuleBuilder, resolve it on the shared _vm, find the entry method, exec, return frame.
    /// No second VM created.
    /// </summary>
    private static CallFrame* CompileAndExec(VeinModuleBuilder module, string testCase, string uid)
    {
        var resolver = _vm->Vault.GetResolver();
        var runtimeModule = resolver.Resolve(new IshtarAssembly(module));
        var entryPoint = runtimeModule->GetSpecialEntryPoint($"master_{testCase}_{uid}() -> [std]::std::Object");

        var args = stackalloc stackval[1];
        var frame = CallFrame.Create(entryPoint, null);
        frame->args = args;
        _vm->exec_method(frame);
        return frame;
    }
    #region IshtarAsyncJob Tests

    [Test]
    public void Job_Create_InitialStatePending()
    {
        var job = IshtarAsyncJob.Create(_vm);

        Assert.That(job->state, Is.EqualTo(JobState.Pending));
        Assert.That((nint)job->continuations, Is.EqualTo(nint.Zero));
        Assert.That((nint)job->vm, Is.EqualTo((nint)_vm));
    }

    [Test]
    public void Job_SetResult_TransitionsToCompleted()
    {
        var job = IshtarAsyncJob.Create(_vm);

        var result = new stackval();
        result.type = VeinTypeCode.TYPE_I4;
        result.data.i = 42;

        job->SetResult(result);

        Assert.That(job->state, Is.EqualTo(JobState.Completed));
        Assert.That(job->result.data.i, Is.EqualTo(42));
        Assert.That(job->result.type, Is.EqualTo(VeinTypeCode.TYPE_I4));
    }

    [Test]
    public void Job_SetCompleted_TransitionsToCompleted_VoidResult()
    {
        var job = IshtarAsyncJob.Create(_vm);
        job->SetCompleted();

        Assert.That(job->state, Is.EqualTo(JobState.Completed));
        Assert.That(job->result.type, Is.EqualTo(VeinTypeCode.TYPE_VOID));
    }

    [Test]
    public void Job_SetException_TransitionsToFaulted()
    {
        var job = IshtarAsyncJob.Create(_vm);

        var ex = new CallFrameException();
        job->SetException(ex);

        Assert.That(job->state, Is.EqualTo(JobState.Faulted));
    }

    [Test]
    public void Job_DoubleResolve_Ignored()
    {
        var job = IshtarAsyncJob.Create(_vm);

        var r1 = new stackval();
        r1.type = VeinTypeCode.TYPE_I4;
        r1.data.i = 10;

        job->SetResult(r1);

        // Second resolve should be ignored
        var r2 = new stackval();
        r2.type = VeinTypeCode.TYPE_I4;
        r2.data.i = 99;
        job->SetResult(r2);

        Assert.That(job->state, Is.EqualTo(JobState.Completed));
        Assert.That(job->result.data.i, Is.EqualTo(10)); // first value preserved
    }

    [Test]
    public void Job_TryRegisterContinuation_WhenPending_ReturnsTrue()
    {
        var job = IshtarAsyncJob.Create(_vm);

        var frame = IshtarGC.AllocateImmortal<SuspendedFrame>(_vm);
        *frame = default;
        frame->vm = _vm;

        var registered = job->TryRegisterContinuation(frame);

        Assert.That(registered, Is.True);
        Assert.That((nint)job->continuations, Is.EqualTo((nint)frame));

        IshtarGC.FreeImmortal(frame);
    }

    [Test]
    public void Job_TryRegisterContinuation_WhenCompleted_ReturnsFalse()
    {
        var job = IshtarAsyncJob.Create(_vm);
        job->SetCompleted();

        var frame = IshtarGC.AllocateImmortal<SuspendedFrame>(_vm);
        *frame = default;
        frame->vm = _vm;

        var registered = job->TryRegisterContinuation(frame);

        Assert.That(registered, Is.False);
        Assert.That((nint)job->continuations, Is.EqualTo(nint.Zero));

        IshtarGC.FreeImmortal(frame);
    }

    [Test]
    public void Job_MultipleContinuations_FormLinkedList()
    {
        var job = IshtarAsyncJob.Create(_vm);

        var f1 = IshtarGC.AllocateImmortal<SuspendedFrame>(_vm);
        *f1 = default;
        f1->vm = _vm;
        var f2 = IshtarGC.AllocateImmortal<SuspendedFrame>(_vm);
        *f2 = default;
        f2->vm = _vm;

        job->TryRegisterContinuation(f1);
        job->TryRegisterContinuation(f2);

        // f2 was prepended, so continuations -> f2 -> f1
        Assert.That((nint)job->continuations, Is.EqualTo((nint)f2));
        Assert.That((nint)f2->next, Is.EqualTo((nint)f1));
        Assert.That((nint)f1->next, Is.EqualTo(nint.Zero));

        IshtarGC.FreeImmortal(f1);
        IshtarGC.FreeImmortal(f2);
    }

    #endregion

    #region SuspendedFrame Capture/Free Tests

    [Test]
    public void CaptureFrame_PreservesFields()
    {
        var evalStack = stackalloc stackval[3];
        evalStack[0].type = VeinTypeCode.TYPE_I4;
        evalStack[0].data.i = 100;
        evalStack[1].type = VeinTypeCode.TYPE_I4;
        evalStack[1].data.i = 200;
        evalStack[2].type = VeinTypeCode.TYPE_I4;
        evalStack[2].data.i = 300;

        var locals = stackalloc stackval[2];
        locals[0].type = VeinTypeCode.TYPE_I4;
        locals[0].data.i = 10;
        locals[1].type = VeinTypeCode.TYPE_I4;
        locals[1].data.i = 20;

        uint fakeIP = 0x42;
        var args = stackalloc stackval[1];
        var ownerJob = IshtarAsyncJob.Create(_vm);
        var awaitedJob = IshtarAsyncJob.Create(_vm);

        var frame = _vm->CaptureFrame(
            null, &fakeIP, evalStack, 3,
            locals, 2,
            null, args, 16,
            ownerJob, awaitedJob);

        Assert.That((nint)frame->savedIP, Is.EqualTo((nint)(&fakeIP)));
        Assert.That(frame->evalStackDepth, Is.EqualTo(3));
        Assert.That(frame->localsCount, Is.EqualTo(2));
        Assert.That(frame->maxStack, Is.EqualTo(16));
        Assert.That((nint)frame->ownerJob, Is.EqualTo((nint)ownerJob));
        Assert.That((nint)frame->awaitedJob, Is.EqualTo((nint)awaitedJob));
        Assert.That((nint)frame->vm, Is.EqualTo((nint)_vm));
        Assert.That((nint)frame->next, Is.EqualTo(nint.Zero));

        // Verify eval stack snapshot (independent copy)
        Assert.That(frame->evalStack[0].data.i, Is.EqualTo(100));
        Assert.That(frame->evalStack[1].data.i, Is.EqualTo(200));
        Assert.That(frame->evalStack[2].data.i, Is.EqualTo(300));

        // Verify locals snapshot
        Assert.That(frame->locals[0].data.i, Is.EqualTo(10));
        Assert.That(frame->locals[1].data.i, Is.EqualTo(20));

        VirtualMachine.FreeSuspendedFrame(frame);
    }

    [Test]
    public void CaptureFrame_EvalStackIsIndependentCopy()
    {
        var evalStack = stackalloc stackval[1];
        evalStack[0].type = VeinTypeCode.TYPE_I4;
        evalStack[0].data.i = 777;

        var frame = _vm->CaptureFrame(
            null, null, evalStack, 1,
            null, 0,
            null, null, 8,
            null, null);

        // Mutate original — captured copy should be unaffected
        evalStack[0].data.i = 999;

        Assert.That(frame->evalStack[0].data.i, Is.EqualTo(777));

        VirtualMachine.FreeSuspendedFrame(frame);
    }

    [Test]
    public void CaptureFrame_ZeroStackAndLocals_NullPointers()
    {
        var frame = _vm->CaptureFrame(
            null, null, null, 0,
            null, 0,
            null, null, 8,
            null, null);

        Assert.That((nint)frame->evalStack, Is.EqualTo(nint.Zero));
        Assert.That((nint)frame->locals, Is.EqualTo(nint.Zero));

        VirtualMachine.FreeSuspendedFrame(frame);
    }

    #endregion

    #region AWAIT Integration Tests (bytecode level)

    private static (VeinModuleBuilder module, VeinCore types, ClassBuilder jobClass) CreateTestModule(string uid)
    {
        var types = new VeinCore();
        var corlibResolver = new IshtarTestModuleResolver();
        corlibResolver.AddSearchPath(new DirectoryInfo("./"));
        var corlib = corlibResolver.ResolveDep("std", new Version(0, 0), new List<VeinModule>());
        var module = new VeinModuleBuilder(new ModuleNameSymbol($"tst_{uid}"), types) { Deps = [corlib] };

        var jobClass = module.DefineClass(
            new QualityTypeName(new NameSymbol("FakeJob"), NamespaceSymbol.Internal, module.Name));
        jobClass.DefineField("_nativeHandle", FieldFlags.Special, types.RawClass);

        return (module, types, jobClass);
    }

    [Test]
    public void Await_CompletedJob_PushesResult_Synchronously()
    {
        var uid = Guid.NewGuid().ToString("N");
        var testCase = "AwaitI4";
        var (module, types, jobClass) = CreateTestModule(uid);

        var cls = module.DefineClass(new NameSymbol($"testClass_{testCase}_{uid}"), NamespaceSymbol.Internal);

        var asyncMethod = cls.DefineMethod("compute_async",
            MethodFlags.Public | MethodFlags.Static | MethodFlags.Async,
            (VeinClass)jobClass);
        asyncMethod.GetGenerator()
            .Emit(OpCodes.LDC_I4_S, 42)
            .Emit(OpCodes.RET);

        var master = cls.DefineMethod($"master_{testCase}_{uid}",
            MethodFlags.Public | MethodFlags.Static,
            VeinTypeCode.TYPE_OBJECT.AsClass()(types));
        master.GetGenerator()
            .Emit(OpCodes.CALL, asyncMethod)
            .Emit(OpCodes.AWAIT)
            .Emit(OpCodes.RET);

        var frame = CompileAndExec(module, testCase, uid);

        if (frame->exception.value != null)
            Assert.Fail($"Fault: [{frame->exception.value->clazz->Name}]");

        Assert.That(frame->returnValue[0].data.i, Is.EqualTo(42));
    }

    [Test]
    public void Await_CompletedVoidJob_ContinuesExecution()
    {
        var uid = Guid.NewGuid().ToString("N");
        var testCase = "AwaitVoid";
        var (module, types, jobClass) = CreateTestModule(uid);

        var cls = module.DefineClass(new NameSymbol($"testClass_{testCase}_{uid}"), NamespaceSymbol.Internal);

        var asyncMethod = cls.DefineMethod("noop_async",
            MethodFlags.Public | MethodFlags.Static | MethodFlags.Async,
            (VeinComplexType)(VeinClass)jobClass);
        asyncMethod.GetGenerator()
            .Emit(OpCodes.RET);

        var master = cls.DefineMethod($"master_{testCase}_{uid}",
            MethodFlags.Public | MethodFlags.Static,
            VeinTypeCode.TYPE_OBJECT.AsClass()(types));
        master.GetGenerator()
            .Emit(OpCodes.CALL, asyncMethod)
            .Emit(OpCodes.AWAIT)
            .Emit(OpCodes.LDC_I4_S, 99)
            .Emit(OpCodes.RET);

        var frame = CompileAndExec(module, testCase, uid);

        if (frame->exception.value != null)
            Assert.Fail($"Fault: [{frame->exception.value->clazz->Name}]");

        Assert.That(frame->returnValue[0].data.i, Is.EqualTo(99));
    }

    [Test]
    public void Await_TwoSequentialAwaits_AddsResults()
    {
        var uid = Guid.NewGuid().ToString("N");
        var testCase = "TwoAwaits";
        var (module, types, jobClass) = CreateTestModule(uid);

        var cls = module.DefineClass(new NameSymbol($"testClass_{testCase}_{uid}"), NamespaceSymbol.Internal);

        // async method returning 10
        var asyncA = cls.DefineMethod("get_ten",
            MethodFlags.Public | MethodFlags.Static | MethodFlags.Async,
            (VeinClass)jobClass);
        asyncA.GetGenerator()
            .Emit(OpCodes.LDC_I4_S, 10)
            .Emit(OpCodes.RET);

        // async method returning 32
        var asyncB = cls.DefineMethod("get_thirtytwo",
            MethodFlags.Public | MethodFlags.Static | MethodFlags.Async,
            (VeinClass)jobClass);
        asyncB.GetGenerator()
            .Emit(OpCodes.LDC_I4_S, 32)
            .Emit(OpCodes.RET);

        // master: call A → await → call B → await → ADD → RET
        var master = cls.DefineMethod($"master_{testCase}_{uid}",
            MethodFlags.Public | MethodFlags.Static,
            VeinTypeCode.TYPE_OBJECT.AsClass()(types));
        master.GetGenerator()
            .Emit(OpCodes.CALL, asyncA)
            .Emit(OpCodes.AWAIT)
            .Emit(OpCodes.CALL, asyncB)
            .Emit(OpCodes.AWAIT)
            .Emit(OpCodes.ADD)
            .Emit(OpCodes.RET);

        var frame = CompileAndExec(module, testCase, uid);

        if (frame->exception.value != null)
            Assert.Fail($"Fault: [{frame->exception.value->clazz->Name}]");

        Assert.That(frame->returnValue[0].data.i, Is.EqualTo(42));
    }

    [Test]
    public void Await_AsyncChain_InnerAsyncCompleteSynchronously()
    {
        // outer async calls inner async, awaits, multiplies result by 2
        var uid = Guid.NewGuid().ToString("N");
        var testCase = "AsyncChain";
        var (module, types, jobClass) = CreateTestModule(uid);

        var cls = module.DefineClass(new NameSymbol($"testClass_{testCase}_{uid}"), NamespaceSymbol.Internal);

        // inner async: returns 7
        var inner = cls.DefineMethod("inner_async",
            MethodFlags.Public | MethodFlags.Static | MethodFlags.Async,
            (VeinClass)jobClass);
        inner.GetGenerator()
            .Emit(OpCodes.LDC_I4_S, 7)
            .Emit(OpCodes.RET);

        // outer async: call inner → await → push 3 → MUL → RET (result = 7 * 3 = 21)
        var outer = cls.DefineMethod("outer_async",
            MethodFlags.Public | MethodFlags.Static | MethodFlags.Async,
            (VeinClass)jobClass);
        outer.GetGenerator()
            .Emit(OpCodes.CALL, inner)
            .Emit(OpCodes.AWAIT)
            .Emit(OpCodes.LDC_I4_S, 3)
            .Emit(OpCodes.MUL)
            .Emit(OpCodes.RET);

        // master: call outer → await → RET
        var master = cls.DefineMethod($"master_{testCase}_{uid}",
            MethodFlags.Public | MethodFlags.Static,
            VeinTypeCode.TYPE_OBJECT.AsClass()(types));
        master.GetGenerator()
            .Emit(OpCodes.CALL, outer)
            .Emit(OpCodes.AWAIT)
            .Emit(OpCodes.RET);

        var frame = CompileAndExec(module, testCase, uid);

        if (frame->exception.value != null)
            Assert.Fail($"Fault: [{frame->exception.value->clazz->Name}]");

        Assert.That(frame->returnValue[0].data.i, Is.EqualTo(21));
    }

    [Test]
    public void Await_PreserveStackBelowAwait()
    {
        // push 100, then call async → await (returns 5), then ADD → result = 105
        var uid = Guid.NewGuid().ToString("N");
        var testCase = "StackPreserve";
        var (module, types, jobClass) = CreateTestModule(uid);

        var cls = module.DefineClass(new NameSymbol($"testClass_{testCase}_{uid}"), NamespaceSymbol.Internal);

        var asyncFive = cls.DefineMethod("get_five",
            MethodFlags.Public | MethodFlags.Static | MethodFlags.Async,
            (VeinClass)jobClass);
        asyncFive.GetGenerator()
            .Emit(OpCodes.LDC_I4_5)
            .Emit(OpCodes.RET);

        // master: LDC 100 → CALL get_five → AWAIT → ADD → RET
        var master = cls.DefineMethod($"master_{testCase}_{uid}",
            MethodFlags.Public | MethodFlags.Static,
            VeinTypeCode.TYPE_OBJECT.AsClass()(types));
        master.GetGenerator()
            .Emit(OpCodes.LDC_I4_S, 100)
            .Emit(OpCodes.CALL, asyncFive)
            .Emit(OpCodes.AWAIT)
            .Emit(OpCodes.ADD)
            .Emit(OpCodes.RET);

        var frame = CompileAndExec(module, testCase, uid);

        if (frame->exception.value != null)
            Assert.Fail($"Fault: [{frame->exception.value->clazz->Name}]");

        Assert.That(frame->returnValue[0].data.i, Is.EqualTo(105));
    }

    [Test]
    public void Await_AsyncReturnsLargeValue()
    {
        // async returns 1_000_000, master multiplies by 2
        var uid = Guid.NewGuid().ToString("N");
        var testCase = "LargeVal";
        var (module, types, jobClass) = CreateTestModule(uid);

        var cls = module.DefineClass(new NameSymbol($"testClass_{testCase}_{uid}"), NamespaceSymbol.Internal);

        var asyncBig = cls.DefineMethod("get_million",
            MethodFlags.Public | MethodFlags.Static | MethodFlags.Async,
            (VeinClass)jobClass);
        asyncBig.GetGenerator()
            .Emit(OpCodes.LDC_I4_S, 1_000_000)
            .Emit(OpCodes.RET);

        var master = cls.DefineMethod($"master_{testCase}_{uid}",
            MethodFlags.Public | MethodFlags.Static,
            VeinTypeCode.TYPE_OBJECT.AsClass()(types));
        master.GetGenerator()
            .Emit(OpCodes.CALL, asyncBig)
            .Emit(OpCodes.AWAIT)
            .Emit(OpCodes.LDC_I4_S, 2)
            .Emit(OpCodes.MUL)
            .Emit(OpCodes.RET);

        var frame = CompileAndExec(module, testCase, uid);

        if (frame->exception.value != null)
            Assert.Fail($"Fault: [{frame->exception.value->clazz->Name}]");

        Assert.That(frame->returnValue[0].data.i, Is.EqualTo(2_000_000));
    }

    [Test]
    public void Await_ThreeChainedAwaits_Arithmetic()
    {
        // (await a + await b) * await c
        // a=3, b=4, c=5 → (3+4)*5 = 35
        var uid = Guid.NewGuid().ToString("N");
        var testCase = "ThreeChain";
        var (module, types, jobClass) = CreateTestModule(uid);

        var cls = module.DefineClass(new NameSymbol($"testClass_{testCase}_{uid}"), NamespaceSymbol.Internal);

        var asyncA = cls.DefineMethod("get_a",
            MethodFlags.Public | MethodFlags.Static | MethodFlags.Async,
            (VeinClass)jobClass);
        asyncA.GetGenerator().Emit(OpCodes.LDC_I4_3).Emit(OpCodes.RET);

        var asyncB = cls.DefineMethod("get_b",
            MethodFlags.Public | MethodFlags.Static | MethodFlags.Async,
            (VeinClass)jobClass);
        asyncB.GetGenerator().Emit(OpCodes.LDC_I4_4).Emit(OpCodes.RET);

        var asyncC = cls.DefineMethod("get_c",
            MethodFlags.Public | MethodFlags.Static | MethodFlags.Async,
            (VeinClass)jobClass);
        asyncC.GetGenerator().Emit(OpCodes.LDC_I4_5).Emit(OpCodes.RET);

        // master: call a → await → call b → await → ADD → call c → await → MUL → RET
        var master = cls.DefineMethod($"master_{testCase}_{uid}",
            MethodFlags.Public | MethodFlags.Static,
            VeinTypeCode.TYPE_OBJECT.AsClass()(types));
        master.GetGenerator()
            .Emit(OpCodes.CALL, asyncA)
            .Emit(OpCodes.AWAIT)
            .Emit(OpCodes.CALL, asyncB)
            .Emit(OpCodes.AWAIT)
            .Emit(OpCodes.ADD)
            .Emit(OpCodes.CALL, asyncC)
            .Emit(OpCodes.AWAIT)
            .Emit(OpCodes.MUL)
            .Emit(OpCodes.RET);

        var frame = CompileAndExec(module, testCase, uid);

        if (frame->exception.value != null)
            Assert.Fail($"Fault: [{frame->exception.value->clazz->Name}]");

        Assert.That(frame->returnValue[0].data.i, Is.EqualTo(35));
    }

    [Test]
    public void Await_DuplicateJobObject_BothAwaitsSeeResult()
    {
        // call async once, DUP the job object, AWAIT twice — both should give 77
        var uid = Guid.NewGuid().ToString("N");
        var testCase = "DupAwait";
        var (module, types, jobClass) = CreateTestModule(uid);

        var cls = module.DefineClass(new NameSymbol($"testClass_{testCase}_{uid}"), NamespaceSymbol.Internal);

        var asyncVal = cls.DefineMethod("get_val",
            MethodFlags.Public | MethodFlags.Static | MethodFlags.Async,
            (VeinClass)jobClass);
        asyncVal.GetGenerator()
            .Emit(OpCodes.LDC_I4_S, 77)
            .Emit(OpCodes.RET);

        // master: call → DUP → AWAIT → swap → AWAIT → ADD → RET = 77 + 77 = 154
        var master = cls.DefineMethod($"master_{testCase}_{uid}",
            MethodFlags.Public | MethodFlags.Static,
            VeinTypeCode.TYPE_OBJECT.AsClass()(types));
        master.GetGenerator()
            .Emit(OpCodes.CALL, asyncVal)
            .Emit(OpCodes.DUP)
            .Emit(OpCodes.AWAIT)  // awaits first copy, pushes 77
            .Emit(OpCodes.CALL, asyncVal) // push new job (just to create separation)
            .Emit(OpCodes.AWAIT)  // awaits second, pushes 77
            .Emit(OpCodes.ADD)    // 77 + 77
            .Emit(OpCodes.RET);

        var frame = CompileAndExec(module, testCase, uid);

        if (frame->exception.value != null)
            Assert.Fail($"Fault: [{frame->exception.value->clazz->Name}]");

        Assert.That(frame->returnValue[0].data.i, Is.EqualTo(154));
    }

    [Test]
    public void Await_AsyncReturnsZero()
    {
        // Edge case: async returns 0 (should not be confused with void/null)
        var uid = Guid.NewGuid().ToString("N");
        var testCase = "RetZero";
        var (module, types, jobClass) = CreateTestModule(uid);

        var cls = module.DefineClass(new NameSymbol($"testClass_{testCase}_{uid}"), NamespaceSymbol.Internal);

        var asyncZero = cls.DefineMethod("get_zero",
            MethodFlags.Public | MethodFlags.Static | MethodFlags.Async,
            (VeinClass)jobClass);
        asyncZero.GetGenerator()
            .Emit(OpCodes.LDC_I4_0)
            .Emit(OpCodes.RET);

        var master = cls.DefineMethod($"master_{testCase}_{uid}",
            MethodFlags.Public | MethodFlags.Static,
            VeinTypeCode.TYPE_OBJECT.AsClass()(types));
        master.GetGenerator()
            .Emit(OpCodes.CALL, asyncZero)
            .Emit(OpCodes.AWAIT)
            .Emit(OpCodes.LDC_I4_1)
            .Emit(OpCodes.ADD)  // 0 + 1 = 1
            .Emit(OpCodes.RET);

        var frame = CompileAndExec(module, testCase, uid);

        if (frame->exception.value != null)
            Assert.Fail($"Fault: [{frame->exception.value->clazz->Name}]");

        Assert.That(frame->returnValue[0].data.i, Is.EqualTo(1));
    }

    [Test]
    public void Await_NestedAsyncThreeDeep()
    {
        // level3 returns 2, level2 awaits level3 and adds 3 (=5),
        // level1 awaits level2 and multiplies by 4 (=20)
        var uid = Guid.NewGuid().ToString("N");
        var testCase = "Nested3";
        var (module, types, jobClass) = CreateTestModule(uid);

        var cls = module.DefineClass(new NameSymbol($"testClass_{testCase}_{uid}"), NamespaceSymbol.Internal);

        var level3 = cls.DefineMethod("level3",
            MethodFlags.Public | MethodFlags.Static | MethodFlags.Async,
            (VeinClass)jobClass);
        level3.GetGenerator()
            .Emit(OpCodes.LDC_I4_2)
            .Emit(OpCodes.RET);

        var level2 = cls.DefineMethod("level2",
            MethodFlags.Public | MethodFlags.Static | MethodFlags.Async,
            (VeinClass)jobClass);
        level2.GetGenerator()
            .Emit(OpCodes.CALL, level3)
            .Emit(OpCodes.AWAIT)
            .Emit(OpCodes.LDC_I4_3)
            .Emit(OpCodes.ADD)
            .Emit(OpCodes.RET);

        var level1 = cls.DefineMethod("level1",
            MethodFlags.Public | MethodFlags.Static | MethodFlags.Async,
            (VeinClass)jobClass);
        level1.GetGenerator()
            .Emit(OpCodes.CALL, level2)
            .Emit(OpCodes.AWAIT)
            .Emit(OpCodes.LDC_I4_4)
            .Emit(OpCodes.MUL)
            .Emit(OpCodes.RET);

        var master = cls.DefineMethod($"master_{testCase}_{uid}",
            MethodFlags.Public | MethodFlags.Static,
            VeinTypeCode.TYPE_OBJECT.AsClass()(types));
        master.GetGenerator()
            .Emit(OpCodes.CALL, level1)
            .Emit(OpCodes.AWAIT)
            .Emit(OpCodes.RET);

        var frame = CompileAndExec(module, testCase, uid);

        if (frame->exception.value != null)
            Assert.Fail($"Fault: [{frame->exception.value->clazz->Name}]");

        Assert.That(frame->returnValue[0].data.i, Is.EqualTo(20));
    }

    [Test]
    public void Await_VoidThenValue_StackCorrect()
    {
        // await void async (no result pushed), then await value async, verify
        var uid = Guid.NewGuid().ToString("N");
        var testCase = "VoidThenVal";
        var (module, types, jobClass) = CreateTestModule(uid);

        var cls = module.DefineClass(new NameSymbol($"testClass_{testCase}_{uid}"), NamespaceSymbol.Internal);

        var asyncVoid = cls.DefineMethod("void_op",
            MethodFlags.Public | MethodFlags.Static | MethodFlags.Async,
            (VeinComplexType)(VeinClass)jobClass);
        asyncVoid.GetGenerator()
            .Emit(OpCodes.RET);

        var asyncVal = cls.DefineMethod("get_55",
            MethodFlags.Public | MethodFlags.Static | MethodFlags.Async,
            (VeinClass)jobClass);
        asyncVal.GetGenerator()
            .Emit(OpCodes.LDC_I4_S, 55)
            .Emit(OpCodes.RET);

        // master: call void_op → await → call get_55 → await → RET (should be 55)
        var master = cls.DefineMethod($"master_{testCase}_{uid}",
            MethodFlags.Public | MethodFlags.Static,
            VeinTypeCode.TYPE_OBJECT.AsClass()(types));
        master.GetGenerator()
            .Emit(OpCodes.CALL, asyncVoid)
            .Emit(OpCodes.AWAIT)
            .Emit(OpCodes.CALL, asyncVal)
            .Emit(OpCodes.AWAIT)
            .Emit(OpCodes.RET);

        var frame = CompileAndExec(module, testCase, uid);

        if (frame->exception.value != null)
            Assert.Fail($"Fault: [{frame->exception.value->clazz->Name}]");

        Assert.That(frame->returnValue[0].data.i, Is.EqualTo(55));
    }

    #endregion

    #region Job Resolution with Continuations (slow path simulation)

    [Test]
    public void Job_SetResult_DispatchesContinuation()
    {
        // Start the job scheduler + thread pool so continuations can execute
        _vm->job_scheduler->StartThread(_vm);
        Thread.Sleep(50); // let loop start

        var job = IshtarAsyncJob.Create(_vm);

        var frame = IshtarGC.AllocateImmortal<SuspendedFrame>(_vm);
        *frame = default;
        frame->vm = _vm;
        frame->ownerJob = null;
        frame->awaitedJob = job;

        // Register continuation while job is pending
        var registered = job->TryRegisterContinuation(frame);
        Assert.That(registered, Is.True);

        // Now resolve the job — this should dispatch the continuation
        var result = new stackval();
        result.type = VeinTypeCode.TYPE_I4;
        result.data.i = 123;
        job->SetResult(result);

        // The continuation was harvested and dispatched
        Assert.That((nint)job->continuations, Is.EqualTo(nint.Zero));
        Assert.That(job->state, Is.EqualTo(JobState.Completed));

        // Give thread pool time to process
        Thread.Sleep(100);

        _vm->job_scheduler->Stop();
    }

    #endregion
}
