//namespace ishtar_test;

//using System;
//using System.Collections.Generic;
//using System.IO;

//public class JitTest : IshtarTestBase
//{
//    public CallFrame Frame => GetVM().Frames.Jit;
//    public IshtarJIT Jit => GetVM().Jit;

//    [Test]
//    [Parallelizable(ParallelScope.None)]
//    public unsafe void CallZeroArgumentStaticVoid()
//    {
//        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//        {
//            Assert.Warn("linux jit is not completed, skip test.");
//            return;
//        }
//        var proc = LoadNativeLibrary("_sample_1");

//        var qwe = Jit.WrapNativeCallStaticVoid(proc, [], null, null, VeinTypeCode.TYPE_I4);

//        ((delegate*<void>)qwe)();
//    }

//    [Test]
//    [Parallelizable(ParallelScope.None)]
//    public unsafe void CallZeroArgumentDirectVoid()
//    {
//        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//        {
//            Assert.Warn("linux jit is not completed, skip test.");
//            return;
//        }
//        var proc = LoadNativeLibrary("_sample_1");

//        var qwe = Jit.WrapNativeCall(proc, [], null, VeinTypeCode.TYPE_I4);

//        ((delegate*<void>)qwe)();
//    }


//    //[Test]
//    //[Parallelizable(ParallelScope.None)]
//    //public unsafe void Call1ArgumentStaticVoid()
//    //{
//    //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//    //    {
//    //        Assert.Warn("linux jit is not completed, skip test.");
//    //        return;
//    //    }
//    //    var proc = LoadNativeLibrary("_sample_2");
//    //    var argsLen = 1;

//    //    var data = GetVM().GC.AllocateStack(Frame, argsLen);

//    //    data[0] = new stackval();
//    //    data[0].type = VeinTypeCode.TYPE_I4;
//    //    data[0].data.i = 228;

//    //    var qwe = Jit.WrapNativeCallStaticVoid(proc, [new VeinArgumentRef("i1", GetVM().Types.Int32Class)], data, null, VeinTypeCode.TYPE_I4);

//    //    ((delegate*<void>)qwe)();

//    //    GetVM().GC.FreeStack(Frame, data, argsLen);
//    //}

//    //[Test]
//    //[Parallelizable(ParallelScope.None)]
//    //public unsafe void Call1ArgumentDirectVoid()
//    //{
//    //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//    //    {
//    //        Assert.Warn("linux jit is not completed, skip test.");
//    //        return;
//    //    }
//    //    var proc = LoadNativeLibrary("_sample_2");

//    //    var qwe = Jit.WrapNativeCall(proc, [new VeinArgumentRef("i1", GetVM().Types.Int32Class)], null, VeinTypeCode.TYPE_I4);

//    //    ((delegate*<int, void>)qwe)(228);
//    //}

//    //[Test]
//    //[Parallelizable(ParallelScope.None)]
//    //public unsafe void Call4ArgumentStaticVoid()
//    //{
//    //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//    //    {
//    //        Assert.Warn("linux jit is not completed, skip test.");
//    //        return;
//    //    }
//    //    var proc = LoadNativeLibrary("_sample_2_1");
//    //    var args = new List<VeinArgumentRef>();
//    //    var vm = GetVM();
//    //    var argsLen = 5;


//    //    var data = vm.GC.AllocateStack(Frame, argsLen);

//    //    for (int i = 0; i != argsLen; i++)
//    //    {
//    //        data[i].type = VeinTypeCode.TYPE_I4;
//    //        data[i].data.i = 228 * (i + 1);
//    //        args.Add(new VeinArgumentRef($"i{i}", vm.Types.Int32Class));
//    //    }

//    //    ((delegate*<void>)Jit.WrapNativeCallStaticVoid(proc, args, data, null, VeinTypeCode.TYPE_I4))();

//    //    vm.GC.FreeStack(Frame, data, argsLen);
//    //}

//    //[Test]
//    //[Parallelizable(ParallelScope.None)]
//    //public unsafe void Call4ArgumentDirectVoid()
//    //{
//    //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//    //    {
//    //        Assert.Warn("linux jit is not completed, skip test.");
//    //        return;
//    //    }
//    //    var proc = LoadNativeLibrary("_sample_2_1");
//    //    var args = new List<VeinArgumentRef>();
//    //    var vm = GetVM();
//    //    ((delegate*<int,int,int,int,void>)Jit.WrapNativeCall(proc, args, null, VeinTypeCode.TYPE_I4))(1,2,3,4);
//    //}

//    //[Test]
//    //[Parallelizable(ParallelScope.None)]
//    //public unsafe void Call5ArgumentStaticVoid()
//    //{
//    //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//    //    {
//    //        Assert.Warn("linux jit is not completed, skip test.");
//    //        return;
//    //    }
//    //    var proc = LoadNativeLibrary("_sample_2_2");
//    //    var args = new List<VeinArgumentRef>();
//    //    var vm = GetVM();
//    //    var argsLen = 5;

//    //    var data = vm.GC.AllocateStack(Frame, argsLen);

//    //    for (int i = 0; i != argsLen; i++)
//    //    {
//    //        data[i].type = VeinTypeCode.TYPE_I4;
//    //        data[i].data.i = 228 * (i + 1);
//    //        args.Add(new VeinArgumentRef($"i{i}", vm.Types.Int32Class));
//    //    }

//    //    ((delegate*<void>)Jit.WrapNativeCallStaticVoid(proc, args, data, null, VeinTypeCode.TYPE_I4))();

//    //    vm.GC.FreeStack(Frame, data, argsLen);
//    //}

//    //[Test]
//    //[Parallelizable(ParallelScope.None)]
//    //public unsafe void Call1ArgumentStaticInt()
//    //{
//    //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//    //    {
//    //        Assert.Warn("linux jit is not completed, skip test.");
//    //        return;
//    //    }
//    //    var proc = LoadNativeLibrary("_sample_3");
//    //    var vm = GetVM();
//    //    var argLen = 1;
//    //    var data = GetVM().GC.AllocateStack(Frame, argLen);
        

//    //    data[0] = new stackval();
//    //    data[0].type = VeinTypeCode.TYPE_I4;
//    //    data[0].data.i = 228;

//    //    var qwe = Jit.WrapNativeCallStaticVoid(proc, [new VeinArgumentRef("i1", vm.Types.Int32Class)], data, null, VeinTypeCode.TYPE_I4);

//    //    vm.GC.FreeStack(Frame, data, argLen);


//    //    Assert.AreEqual(228, ((delegate*<int>)qwe)());
//    //}

//    //[Test]
//    //[Parallelizable(ParallelScope.None)]
//    //public unsafe void Call1ArgumentDirectInt()
//    //{
//    //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//    //    {
//    //        Assert.Warn("linux jit is not completed, skip test.");
//    //        return;
//    //    }
//    //    var proc = LoadNativeLibrary("_sample_3");
//    //    var vm = GetVM();

//    //    var qwe = Jit.WrapNativeCall(proc, [new VeinArgumentRef("i1", vm.Types.Int32Class)], null, VeinTypeCode.TYPE_I4);

//    //    Assert.AreEqual(228, ((delegate*<int, int>)qwe)(228));
//    //}

//    //[Test]
//    //[Parallelizable(ParallelScope.None)]
//    //public unsafe void UseReturnMemoryCall1ArgumentStaticInt()
//    //{
//    //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//    //    {
//    //        Assert.Warn("linux jit is not completed, skip test.");
//    //        return;
//    //    }
//    //    var proc = LoadNativeLibrary("_sample_3");


//    //    var data = GetVM().GC.AllocateStack(Frame, 1);

//    //    data[0] = new stackval();
//    //    data[0].type = VeinTypeCode.TYPE_I4;
//    //    data[0].data.i = 228;


//    //    var retMem = NativeMemory.AllocZeroed(100);

//    //    var qwe = Jit.WrapNativeCallStaticVoid(proc, [new VeinArgumentRef("i1", GetVM().Types.Int32Class)], data, retMem, VeinTypeCode.TYPE_I4);

//    //    ((delegate*<void>)qwe)();

//    //    var outMem = (int*)retMem;

//    //    GetVM().GC.FreeStack(Frame, data, 1);

//    //    Assert.AreEqual(228, outMem[0]);
//    //}

//    //[Test]
//    //[Parallelizable(ParallelScope.None)]
//    //public unsafe void UseReturnMemoryCall1ArgumentDirectInt()
//    //{
//    //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//    //    {
//    //        Assert.Warn("linux jit is not completed, skip test.");
//    //        return;
//    //    }
//    //    var proc = LoadNativeLibrary("_sample_3");
        

//    //    var retMem = NativeMemory.AllocZeroed(100);

//    //    var qwe = Jit.WrapNativeCall(proc, [new VeinArgumentRef("i1", GetVM().Types.Int32Class)], retMem, VeinTypeCode.TYPE_I4);

//    //    ((delegate*<int,void>)qwe)(228);

//    //    var outMem = (int*)retMem;
        
//    //    Assert.AreEqual(228, outMem[0]);
//    //}

//    //[Test]
//    //[Parallelizable(ParallelScope.None)]
//    //public unsafe void UseReturnMemoryCall1ArgumentDirectLong()
//    //{
//    //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//    //    {
//    //        Assert.Warn("linux jit is not completed, skip test.");
//    //        return;
//    //    }
//    //    var proc = LoadNativeLibrary("_sample_7");
        
//    //    var retMem = NativeMemory.AllocZeroed(100);

//    //    var qwe = Jit.WrapNativeCall(proc, [new VeinArgumentRef("l1", GetVM().Types.Int64Class)], retMem, VeinTypeCode.TYPE_I8);

//    //    ((delegate*<long, void>)qwe)(666_666_666_666);

//    //    var outMem = (long*)retMem;

//    //    Assert.AreEqual(666_666_666_666, outMem[0]);
//    //}

//    //[Test]
//    //[Parallelizable(ParallelScope.None)]
//    //public unsafe void UseReturnMemoryCall5ArgumentDirectFloat()
//    //{
//    //    var proc = LoadNativeLibrary("_sample_8");

//    //    var retMem = NativeMemory.AllocZeroed(100);

//    //    var qwe = IshtarJIT.WrapNativeCall(GetVM(), proc, [
//    //        new VeinArgumentRef("r1", GetVM().Types.FloatClass),
//    //        new VeinArgumentRef("r2", GetVM().Types.FloatClass),
//    //        new VeinArgumentRef("r3", GetVM().Types.FloatClass),
//    //        new VeinArgumentRef("r4", GetVM().Types.FloatClass),
//    //        new VeinArgumentRef("r5", GetVM().Types.FloatClass),
//    //    ], retMem, VeinTypeCode.TYPE_R4);

//    //    ((delegate*<float, float, float, float, float, void>)qwe)(MathF.Tau, MathF.E, MathF.PI, 1, 2);

//    //    var outMem = (float*)retMem;

//    //    Assert.AreEqual(MathF.Tau + MathF.E + MathF.PI + 1 + 2, outMem[0]);
//    //}

//    private static nint LoadNativeLibrary(string procedure)
//    {
//        var ptr = NativeLibrary.Load(new FileInfo("./sample_native_library.dll").FullName);
//        return NativeLibrary.GetExport(ptr, procedure);
//    }
//}
