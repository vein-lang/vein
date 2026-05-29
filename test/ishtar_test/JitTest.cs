namespace ishtar_test;

using System;
using System.IO;
using System.Runtime.InteropServices;
using ishtar;
using vein.runtime;
using NUnit.Framework;

/// <summary>
/// Tests for NativeCallMarshaller JIT trampolines.
/// Each test loads the sample_native_library AOT dll, resolves a native export,
/// compiles a trampoline via NativeCallMarshaller, and invokes it through stackval I/O.
/// </summary>
public unsafe class JitTest
{
    private static nint _libHandle;

    [OneTimeSetUp]
    public static void Setup()
    {
        var libPath = Path.GetFullPath("./sample_native_library.dll");
        Assert.That(File.Exists(libPath), Is.True, $"sample_native_library.dll not found at {libPath}");
        _libHandle = NativeLibrary.Load(libPath);
    }

    private static nint GetExport(string name) => NativeLibrary.GetExport(_libHandle, name);

    // ═══════════════════════════════════════════════════════════════════
    // _sample_1: void() — zero args, void return
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void CallVoidNoArgs()
    {
        var proc = GetExport("_sample_1");
        var trampoline = CompileTrampoline(proc, [], VeinTypeCode.TYPE_VOID);

        var result = new stackval();
        trampoline(null, &result);

        Assert.That(result.type, Is.EqualTo(VeinTypeCode.TYPE_VOID));
    }

    // ═══════════════════════════════════════════════════════════════════
    // _sample_2: void(int) — one int arg, void return
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void CallVoidOneIntArg()
    {
        var proc = GetExport("_sample_2");
        var trampoline = CompileTrampoline(proc, [VeinTypeCode.TYPE_I4], VeinTypeCode.TYPE_VOID);

        var args = stackalloc stackval[1];
        args[0].data.i = 42;
        args[0].type = VeinTypeCode.TYPE_I4;

        var result = new stackval();
        trampoline(args, &result);

        Assert.That(result.type, Is.EqualTo(VeinTypeCode.TYPE_VOID));
    }

    // ═══════════════════════════════════════════════════════════════════
    // _sample_3: int(int) — one int arg, int return (identity)
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void CallIntOneIntArg_ReturnsValue()
    {
        var proc = GetExport("_sample_3");
        var trampoline = CompileTrampoline(proc, [VeinTypeCode.TYPE_I4], VeinTypeCode.TYPE_I4);

        var args = stackalloc stackval[1];
        args[0].data.i = 228;
        args[0].type = VeinTypeCode.TYPE_I4;

        var result = new stackval();
        trampoline(args, &result);

        Assert.That(result.type, Is.EqualTo(VeinTypeCode.TYPE_I4));
        Assert.That(result.data.i, Is.EqualTo(228));
    }

    // ═══════════════════════════════════════════════════════════════════
    // _sample_2_1: void(int, int, int, int) — 4 int args in registers
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void CallVoidFourIntArgs()
    {
        var proc = GetExport("_sample_2_1");
        var argTypes = new[] { VeinTypeCode.TYPE_I4, VeinTypeCode.TYPE_I4, VeinTypeCode.TYPE_I4, VeinTypeCode.TYPE_I4 };
        var trampoline = CompileTrampoline(proc, argTypes, VeinTypeCode.TYPE_VOID);

        var args = stackalloc stackval[4];
        for (var i = 0; i < 4; i++)
        {
            args[i].data.i = 100 * (i + 1);
            args[i].type = VeinTypeCode.TYPE_I4;
        }

        var result = new stackval();
        trampoline(args, &result);

        Assert.That(result.type, Is.EqualTo(VeinTypeCode.TYPE_VOID));
    }

    // ═══════════════════════════════════════════════════════════════════
    // _sample_2_2: void(int, int, int, int, int) — 5 int args (one on stack on Windows)
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void CallVoidFiveIntArgs_StackOverflow()
    {
        var proc = GetExport("_sample_2_2");
        var argTypes = new[]
        {
            VeinTypeCode.TYPE_I4, VeinTypeCode.TYPE_I4, VeinTypeCode.TYPE_I4,
            VeinTypeCode.TYPE_I4, VeinTypeCode.TYPE_I4
        };
        var trampoline = CompileTrampoline(proc, argTypes, VeinTypeCode.TYPE_VOID);

        var args = stackalloc stackval[5];
        for (var i = 0; i < 5; i++)
        {
            args[i].data.i = 228 * (i + 1);
            args[i].type = VeinTypeCode.TYPE_I4;
        }

        var result = new stackval();
        trampoline(args, &result);

        Assert.That(result.type, Is.EqualTo(VeinTypeCode.TYPE_VOID));
    }

    // ═══════════════════════════════════════════════════════════════════
    // _sample_2_3: int(int, int, int, int, int) — returns sum of first 4
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void CallInt_FiveIntArgs_ReturnsSumOfFirst4()
    {
        var proc = GetExport("_sample_2_3");
        var argTypes = new[]
        {
            VeinTypeCode.TYPE_I4, VeinTypeCode.TYPE_I4, VeinTypeCode.TYPE_I4,
            VeinTypeCode.TYPE_I4, VeinTypeCode.TYPE_I4
        };
        var trampoline = CompileTrampoline(proc, argTypes, VeinTypeCode.TYPE_I4);

        var args = stackalloc stackval[5];
        args[0].data.i = 10;
        args[1].data.i = 20;
        args[2].data.i = 30;
        args[3].data.i = 40;
        args[4].data.i = 50;
        for (var i = 0; i < 5; i++)
            args[i].type = VeinTypeCode.TYPE_I4;

        var result = new stackval();
        trampoline(args, &result);

        Assert.That(result.type, Is.EqualTo(VeinTypeCode.TYPE_I4));
        Assert.That(result.data.i, Is.EqualTo(10 + 20 + 30 + 40)); // returns i1+i2+i3+i4
    }

    // ═══════════════════════════════════════════════════════════════════
    // _sample_7: long(long) — long arg/return (identity)
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void CallLongOneLongArg_ReturnsValue()
    {
        var proc = GetExport("_sample_7");
        var trampoline = CompileTrampoline(proc, [VeinTypeCode.TYPE_I8], VeinTypeCode.TYPE_I8);

        var args = stackalloc stackval[1];
        args[0].data.l = 666_666_666_666L;
        args[0].type = VeinTypeCode.TYPE_I8;

        var result = new stackval();
        trampoline(args, &result);

        Assert.That(result.type, Is.EqualTo(VeinTypeCode.TYPE_I8));
        Assert.That(result.data.l, Is.EqualTo(666_666_666_666L));
    }

    // ═══════════════════════════════════════════════════════════════════
    // _sample_7_1: long(long, long) — returns i1 + i2
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void CallLongTwoLongArgs_ReturnsSum()
    {
        var proc = GetExport("_sample_7_1");
        var argTypes = new[] { VeinTypeCode.TYPE_I8, VeinTypeCode.TYPE_I8 };
        var trampoline = CompileTrampoline(proc, argTypes, VeinTypeCode.TYPE_I8);

        var args = stackalloc stackval[2];
        args[0].data.l = 100_000_000_000L;
        args[0].type = VeinTypeCode.TYPE_I8;
        args[1].data.l = 200_000_000_000L;
        args[1].type = VeinTypeCode.TYPE_I8;

        var result = new stackval();
        trampoline(args, &result);

        Assert.That(result.type, Is.EqualTo(VeinTypeCode.TYPE_I8));
        Assert.That(result.data.l, Is.EqualTo(300_000_000_000L));
    }

    // ═══════════════════════════════════════════════════════════════════
    // _sample_7_2: long(long, long, long, long) — returns sum of 4
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void CallLongFourLongArgs_ReturnsSum()
    {
        var proc = GetExport("_sample_7_2");
        var argTypes = new[] { VeinTypeCode.TYPE_I8, VeinTypeCode.TYPE_I8, VeinTypeCode.TYPE_I8, VeinTypeCode.TYPE_I8 };
        var trampoline = CompileTrampoline(proc, argTypes, VeinTypeCode.TYPE_I8);

        var args = stackalloc stackval[4];
        args[0].data.l = 1L;
        args[0].type = VeinTypeCode.TYPE_I8;
        args[1].data.l = 2L;
        args[1].type = VeinTypeCode.TYPE_I8;
        args[2].data.l = 3L;
        args[2].type = VeinTypeCode.TYPE_I8;
        args[3].data.l = 4L;
        args[3].type = VeinTypeCode.TYPE_I8;

        var result = new stackval();
        trampoline(args, &result);

        Assert.That(result.type, Is.EqualTo(VeinTypeCode.TYPE_I8));
        Assert.That(result.data.l, Is.EqualTo(10L));
    }

    // ═══════════════════════════════════════════════════════════════════
    // _sample_2_4: long(long, long, long, long, long) — 5 long args, sum of first 4
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void CallLongFiveLongArgs_StackOverflow_ReturnsSum()
    {
        var proc = GetExport("_sample_2_4");
        var argTypes = new[] { VeinTypeCode.TYPE_I8, VeinTypeCode.TYPE_I8, VeinTypeCode.TYPE_I8, VeinTypeCode.TYPE_I8, VeinTypeCode.TYPE_I8 };
        var trampoline = CompileTrampoline(proc, argTypes, VeinTypeCode.TYPE_I8);

        var args = stackalloc stackval[5];
        args[0].data.l = 10L;
        args[0].type = VeinTypeCode.TYPE_I8;
        args[1].data.l = 20L;
        args[1].type = VeinTypeCode.TYPE_I8;
        args[2].data.l = 30L;
        args[2].type = VeinTypeCode.TYPE_I8;
        args[3].data.l = 40L;
        args[3].type = VeinTypeCode.TYPE_I8;
        args[4].data.l = 50L;
        args[4].type = VeinTypeCode.TYPE_I8;

        var result = new stackval();
        trampoline(args, &result);

        Assert.That(result.type, Is.EqualTo(VeinTypeCode.TYPE_I8));
        Assert.That(result.data.l, Is.EqualTo(100L)); // sum of first 4 = 10+20+30+40
    }

    // ═══════════════════════════════════════════════════════════════════
    // _sample_8: float(float, float, float, float) — returns sum
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void CallFloat_FourFloatArgs_ReturnsSum()
    {
        var proc = GetExport("_sample_8");
        var argTypes = new[] { VeinTypeCode.TYPE_R4, VeinTypeCode.TYPE_R4, VeinTypeCode.TYPE_R4, VeinTypeCode.TYPE_R4 };
        var trampoline = CompileTrampoline(proc, argTypes, VeinTypeCode.TYPE_R4);

        var args = stackalloc stackval[4];
        args[0].data.f_r4 = 1.5f;
        args[0].type = VeinTypeCode.TYPE_R4;
        args[1].data.f_r4 = 2.5f;
        args[1].type = VeinTypeCode.TYPE_R4;
        args[2].data.f_r4 = 3.0f;
        args[2].type = VeinTypeCode.TYPE_R4;
        args[3].data.f_r4 = 4.0f;
        args[3].type = VeinTypeCode.TYPE_R4;

        var result = new stackval();
        trampoline(args, &result);

        Assert.That(result.type, Is.EqualTo(VeinTypeCode.TYPE_R4));
        Assert.That(result.data.f_r4, Is.EqualTo(1.5f + 2.5f + 3.0f + 4.0f).Within(0.001f));
    }

    // ═══════════════════════════════════════════════════════════════════
    // _sample_8_1: float() — no args, returns constant 14.48f
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void CallFloat_NoArgs_ReturnsConstant()
    {
        var proc = GetExport("_sample_8_1");
        var trampoline = CompileTrampoline(proc, [], VeinTypeCode.TYPE_R4);

        var result = new stackval();
        trampoline(null, &result);

        Assert.That(result.type, Is.EqualTo(VeinTypeCode.TYPE_R4));
        Assert.That(result.data.f_r4, Is.EqualTo(14.48f).Within(0.001f));
    }

    // ═══════════════════════════════════════════════════════════════════
    // _sample_9: double(double, double, double, double) — returns sum
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void CallDouble_FourDoubleArgs_ReturnsSum()
    {
        var proc = GetExport("_sample_9");
        var argTypes = new[] { VeinTypeCode.TYPE_R8, VeinTypeCode.TYPE_R8, VeinTypeCode.TYPE_R8, VeinTypeCode.TYPE_R8 };
        var trampoline = CompileTrampoline(proc, argTypes, VeinTypeCode.TYPE_R8);

        var args = stackalloc stackval[4];
        args[0].data.f = 1.1;
        args[0].type = VeinTypeCode.TYPE_R8;
        args[1].data.f = 2.2;
        args[1].type = VeinTypeCode.TYPE_R8;
        args[2].data.f = 3.3;
        args[2].type = VeinTypeCode.TYPE_R8;
        args[3].data.f = 4.4;
        args[3].type = VeinTypeCode.TYPE_R8;

        var result = new stackval();
        trampoline(args, &result);

        Assert.That(result.type, Is.EqualTo(VeinTypeCode.TYPE_R8));
        Assert.That(result.data.f, Is.EqualTo(1.1 + 2.2 + 3.3 + 4.4).Within(0.0001));
    }

    // ═══════════════════════════════════════════════════════════════════
    // ExecutableMemory basic test — alloc/free doesn't crash
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void ExecutableMemory_AllocAndFree()
    {
        // Simple x64: mov eax, 42; ret
        byte[] code = [0xB8, 0x2A, 0x00, 0x00, 0x00, 0xC3];
        var ptr = ExecutableMemory.Alloc(code);
        Assert.That(ptr != null);

        var fn = (delegate* unmanaged[SuppressGCTransition]<int>)ptr;
        var val = fn();
        Assert.That(val, Is.EqualTo(42));

        ExecutableMemory.Free(ptr, (nuint)code.Length);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Helper: compile trampoline from raw type arrays
    // ═══════════════════════════════════════════════════════════════════

    private static delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>
        CompileTrampoline(nint targetFn, VeinTypeCode[] argTypes, VeinTypeCode returnType)
    {
        fixed (VeinTypeCode* pArgTypes = argTypes)
        {
            var ptr = NativeCallMarshaller.CompileTrampoline(targetFn, pArgTypes, argTypes.Length, returnType);
            return (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)ptr;
        }
    }
}

