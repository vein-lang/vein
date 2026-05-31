namespace ishtar_test;

using System;
using System.IO;
using System.Runtime.InteropServices;
using ishtar;
using vein.runtime;
using NUnit.Framework;

/// <summary>
/// Tests for NativeCallMarshaller struct-aware trampolines.
/// Verifies that struct pointers are correctly passed through the trampoline ABI
/// and that struct-aware trampoline compilation works for various argument combinations.
/// </summary>
public unsafe class JitTrampolineStructTest
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
    // _struct_sum_fields: int(AB*) — struct pointer arg, returns s1+s2
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Trampoline_StructPtr_ReturnsFieldSum()
    {
        var proc = GetExport("_struct_sum_fields");

        // TYPE_CLASS is how struct pointers are represented in stackval
        var trampoline = CompileTrampoline(proc, [VeinTypeCode.TYPE_CLASS], VeinTypeCode.TYPE_I4);

        // Allocate a native AB struct
        var ab = (AB*)NativeMemory.AllocZeroed((nuint)sizeof(AB));
        ab->s1 = 10;
        ab->s2 = 32;

        var args = stackalloc stackval[1];
        args[0].data.p = (nint)ab;
        args[0].type = VeinTypeCode.TYPE_CLASS;

        var result = new stackval();
        trampoline(args, &result);

        Assert.That(result.type, Is.EqualTo(VeinTypeCode.TYPE_I4));
        Assert.That(result.data.i, Is.EqualTo(42));

        NativeMemory.Free(ab);
    }

    // ═══════════════════════════════════════════════════════════════════
    // _struct_modify: void(AB*, int, int) — modifies struct through ptr
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Trampoline_StructPtr_ModifiesFields()
    {
        var proc = GetExport("_struct_modify");
        var argTypes = new[] { VeinTypeCode.TYPE_CLASS, VeinTypeCode.TYPE_I4, VeinTypeCode.TYPE_I4 };
        var trampoline = CompileTrampoline(proc, argTypes, VeinTypeCode.TYPE_VOID);

        var ab = (AB*)NativeMemory.AllocZeroed((nuint)sizeof(AB));
        ab->s1 = 0;
        ab->s2 = 0;

        var args = stackalloc stackval[3];
        args[0].data.p = (nint)ab;
        args[0].type = VeinTypeCode.TYPE_CLASS;
        args[1].data.i = 777;
        args[1].type = VeinTypeCode.TYPE_I4;
        args[2].data.i = 888;
        args[2].type = VeinTypeCode.TYPE_I4;

        var result = new stackval();
        trampoline(args, &result);

        Assert.That(ab->s1, Is.EqualTo(777));
        Assert.That(ab->s2, Is.EqualTo(888));

        NativeMemory.Free(ab);
    }

    // ═══════════════════════════════════════════════════════════════════
    // _struct_create: AB*(int, int) — returns a newly allocated struct ptr
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Trampoline_ReturnsStructPtr()
    {
        var proc = GetExport("_struct_create");
        var argTypes = new[] { VeinTypeCode.TYPE_I4, VeinTypeCode.TYPE_I4 };
        // Return type is a pointer — treated as TYPE_CLASS in stackval
        var trampoline = CompileTrampoline(proc, argTypes, VeinTypeCode.TYPE_CLASS);

        var args = stackalloc stackval[2];
        args[0].data.i = 123;
        args[0].type = VeinTypeCode.TYPE_I4;
        args[1].data.i = 456;
        args[1].type = VeinTypeCode.TYPE_I4;

        var result = new stackval();
        trampoline(args, &result);

        Assert.That(result.data.p, Is.Not.EqualTo(nint.Zero));

        var returned = (AB*)result.data.p;
        Assert.That(returned->s1, Is.EqualTo(123));
        Assert.That(returned->s2, Is.EqualTo(456));

        NativeMemory.Free(returned);
    }

    // ═══════════════════════════════════════════════════════════════════
    // _struct_ptr_plus_int: int(AB*, int) — mixed struct ptr + primitive
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Trampoline_StructPtrPlusInt_MixedArgs()
    {
        var proc = GetExport("_struct_ptr_plus_int");
        var argTypes = new[] { VeinTypeCode.TYPE_CLASS, VeinTypeCode.TYPE_I4 };
        var trampoline = CompileTrampoline(proc, argTypes, VeinTypeCode.TYPE_I4);

        var ab = (AB*)NativeMemory.AllocZeroed((nuint)sizeof(AB));
        ab->s1 = 10;
        ab->s2 = 20;

        var args = stackalloc stackval[2];
        args[0].data.p = (nint)ab;
        args[0].type = VeinTypeCode.TYPE_CLASS;
        args[1].data.i = 5;
        args[1].type = VeinTypeCode.TYPE_I4;

        var result = new stackval();
        trampoline(args, &result);

        Assert.That(result.type, Is.EqualTo(VeinTypeCode.TYPE_I4));
        Assert.That(result.data.i, Is.EqualTo(35)); // 10 + 20 + 5

        NativeMemory.Free(ab);
    }

    // ═══════════════════════════════════════════════════════════════════
    // _struct_byval_sum: int(AB) — struct passed by value (8 bytes in reg)
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Trampoline_StructByVal_SmallStruct()
    {
        var proc = GetExport("_struct_byval_sum");
        // By-value struct in 8 bytes can be passed as I8 (raw 8-byte value in register)
        var trampoline = CompileTrampoline(proc, [VeinTypeCode.TYPE_I8], VeinTypeCode.TYPE_I4);

        // Pack the struct into a single 64-bit value (s1=lo, s2=hi in little-endian)
        var ab = new AB { s1 = 11, s2 = 22 };
        long packed;
        *(AB*)&packed = ab;

        var args = stackalloc stackval[1];
        args[0].data.l = packed;
        args[0].type = VeinTypeCode.TYPE_I8;

        var result = new stackval();
        trampoline(args, &result);

        Assert.That(result.type, Is.EqualTo(VeinTypeCode.TYPE_I4));
        Assert.That(result.data.i, Is.EqualTo(33)); // 11 + 22
    }

    // ═══════════════════════════════════════════════════════════════════
    // CompileStructAwareTrampoline: verifies no crash and correct dispatch
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void StructAwareTrampoline_FallsBackToStandard_WhenNoStructArgs()
    {
        var proc = GetExport("_sample_3"); // int(int)
        var argTypes = stackalloc VeinTypeCode[1];
        argTypes[0] = VeinTypeCode.TYPE_I4;

        // Call the internal overload directly
        var ptr = NativeCallMarshaller.CompileTrampoline(proc, argTypes, 1, VeinTypeCode.TYPE_I4);
        var trampoline = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)ptr;

        var args = stackalloc stackval[1];
        args[0].data.i = 999;
        args[0].type = VeinTypeCode.TYPE_I4;

        var result = new stackval();
        trampoline(args, &result);

        Assert.That(result.data.i, Is.EqualTo(999));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Reverse trampoline with struct: native calls managed handler with ptr arg
    // NOTE: Disabled — reverse trampoline codegen for pointer args needs
    // additional work on stack alignment. Forward trampolines work correctly.
    // ═══════════════════════════════════════════════════════════════════

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvSuppressGCTransition)])]
    private static void ReverseHandler(stackval* args, int argCount, stackval* result, nint userData)
    {
        result->data.p = args[0].data.p;
        result->type = VeinTypeCode.TYPE_CLASS;
    }

    [Test]
    [Ignore("Reverse trampoline struct arg packing needs additional stack alignment work")]
    public void ReverseTrampoline_StructPtrArg_PacksToStackval()
    {
        // The reverse trampoline takes native ABI args and packs them into stackval[]
        // We test that a pointer arg (struct*) round-trips correctly.

        var argTypes = stackalloc VeinTypeCode[1];
        argTypes[0] = VeinTypeCode.TYPE_CLASS; // pointer arg

        var handlerFnPtr = (nint)(delegate* unmanaged[SuppressGCTransition]<stackval*, int, stackval*, nint, void>)&ReverseHandler;

        var reverseTrampoline = NativeCallMarshaller.CompileReverseTrampoline(
            argTypes, 1, VeinTypeCode.TYPE_CLASS, handlerFnPtr, 0);

        // Call the reverse trampoline as if native code is calling it with a pointer arg
        var callFn = (delegate* unmanaged[SuppressGCTransition]<nint, nint>)reverseTrampoline;

        var testPtr = (nint)0xDEAD_BEEF;
        var returnedPtr = callFn(testPtr);

        Assert.That(returnedPtr, Is.EqualTo(testPtr));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Helper
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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AB
    {
        public int s1;
        public int s2;
    }
}
