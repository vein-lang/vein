namespace ishtar_test;

using ishtar;
using ishtar.emit;
using vein.runtime;

[TestFixture]
[Parallelizable(ParallelScope.None)]
public unsafe class UnsafeRuntimeTest : IshtarTestBase
{
    [Test]
    [Parallelizable(ParallelScope.None)]
    public void STACKALLOC_WriteRead_I32()
    {
        using var scope = CreateScope();

        scope.OnCodeBuild((gen, ctx) =>
        {
            var rawClass = VeinTypeCode.TYPE_RAW.AsClass()(scope.Types);

            gen.EnsureLocal("ptr", rawClass);

            // Allocate 4 bytes on the unsafe stack
            gen.Emit(OpCodes.LDC_I4_S, 4);
            gen.Emit(OpCodes.STACKALLOC);
            // stack: [ptr:RAW]

            gen.Emit(OpCodes.STLOC_0);
            // local[0] = ptr

            // Write 42 through the pointer: (value, ptr) -> ()
            gen.Emit(OpCodes.LDC_I4_S, 42);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STIND, (int)VeinTypeCode.TYPE_I4);

            // Read back: (ptr) -> (value)
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.LDIND, (int)VeinTypeCode.TYPE_I4);

            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();

        Assert.That(result->returnValue[0].data.i, Is.EqualTo(42));
        Assert.That(result->returnValue[0].type, Is.EqualTo(VeinTypeCode.TYPE_I4));
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void STACKALLOC_WriteTwoValues_ReadSecond()
    {
        using var scope = CreateScope();

        scope.OnCodeBuild((gen, ctx) =>
        {
            var rawClass = VeinTypeCode.TYPE_RAW.AsClass()(scope.Types);
            var i32Class = VeinTypeCode.TYPE_I4.AsClass()(scope.Types);

            gen.EnsureLocal("ptr", rawClass);
            gen.EnsureLocal("ptr2", rawClass);

            // Allocate 8 bytes (two i32)
            gen.Emit(OpCodes.LDC_I4_S, 8);
            gen.Emit(OpCodes.STACKALLOC);
            gen.Emit(OpCodes.STLOC_0);

            // Write 10 at ptr+0
            gen.Emit(OpCodes.LDC_I4_S, 10);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STIND, (int)VeinTypeCode.TYPE_I4);

            // Compute ptr+4 (second element offset)
            gen.Emit(OpCodes.LDLOC_0);         // ptr
            gen.Emit(OpCodes.LDC_I4_S, 4);     // 4
            gen.Emit(OpCodes.ADD);              // ptr + 4
            gen.Emit(OpCodes.STLOC_1);          // local[1] = ptr+4

            // Write 20 at ptr+4
            gen.Emit(OpCodes.LDC_I4_S, 20);
            gen.Emit(OpCodes.LDLOC_1);
            gen.Emit(OpCodes.STIND, (int)VeinTypeCode.TYPE_I4);

            // Read from ptr+4 -> should be 20
            gen.Emit(OpCodes.LDLOC_1);
            gen.Emit(OpCodes.LDIND, (int)VeinTypeCode.TYPE_I4);

            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();

        Assert.That(result->returnValue[0].data.i, Is.EqualTo(20));
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void ADDR_LOC_ReadLocalThroughPointer()
    {
        using var scope = CreateScope();

        scope.OnCodeBuild((gen, ctx) =>
        {
            var i32Class = VeinTypeCode.TYPE_I4.AsClass()(scope.Types);
            var rawClass = VeinTypeCode.TYPE_RAW.AsClass()(scope.Types);

            gen.EnsureLocal("x", i32Class);
            gen.EnsureLocal("ptr", rawClass);

            // x = 99
            gen.Emit(OpCodes.LDC_I4_S, 99);
            gen.Emit(OpCodes.STLOC_0);

            // ptr = &x (address of local 0)
            gen.Emit(OpCodes.ADDR_LOC, 0);
            gen.Emit(OpCodes.STLOC_1);

            // *ptr → should be 99
            gen.Emit(OpCodes.LDLOC_1);
            gen.Emit(OpCodes.LDIND, (int)VeinTypeCode.TYPE_I4);

            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();

        Assert.That(result->returnValue[0].data.i, Is.EqualTo(99));
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void ADDR_LOC_WriteLocalThroughPointer()
    {
        using var scope = CreateScope();

        scope.OnCodeBuild((gen, ctx) =>
        {
            var i32Class = VeinTypeCode.TYPE_I4.AsClass()(scope.Types);
            var rawClass = VeinTypeCode.TYPE_RAW.AsClass()(scope.Types);

            gen.EnsureLocal("x", i32Class);
            gen.EnsureLocal("ptr", rawClass);

            // x = 0 (initial)
            gen.Emit(OpCodes.LDC_I4_0);
            gen.Emit(OpCodes.STLOC_0);

            // ptr = &x
            gen.Emit(OpCodes.ADDR_LOC, 0);
            gen.Emit(OpCodes.STLOC_1);

            // *ptr = 77 (write through pointer)
            gen.Emit(OpCodes.LDC_I4_S, 77);
            gen.Emit(OpCodes.LDLOC_1);
            gen.Emit(OpCodes.STIND, (int)VeinTypeCode.TYPE_I4);

            // Return x (read directly)
            gen.Emit(OpCodes.LDLOC_0);

            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();

        Assert.That(result->returnValue[0].data.i, Is.EqualTo(77));
    }
}
