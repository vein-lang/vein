namespace ishtar_test
{
    using System;
    using System.Reflection;
    using ishtar;
    using mana.runtime;
    using Xunit;
    using static mana.runtime.ManaTypeCode;

    public unsafe class InterfacesTest : IshtarTestBase
    {
        [Fact()]
        public void InitVTableTest()
        {
            using var ctx = CreateContext();

            QualityTypeName type = $"tst%global::foo/bar/IBaz";

            ctx.EnsureType(type);

            var A1 = new RuntimeIshtarInterface(type, Array.Empty<RuntimeIshtarInterface>(), _module);

            var FooA1 = A1.DefineMethod("FooA1", TYPE_VOID.AsRuntimeClass(), MethodFlags.Public);

            var FieldA1 = A1.DefineField("FieldA1", FieldFlags.Public, TYPE_I4.AsRuntimeClass());


            var A2 = new RuntimeIshtarInterface(type, Array.Empty<RuntimeIshtarInterface>(), _module);

            var FooA2 = A2.DefineMethod("FooA2", TYPE_VOID.AsRuntimeClass(), MethodFlags.Public);

            var FieldA2 = A2.DefineField("FieldA2", FieldFlags.Public, TYPE_I4.AsRuntimeClass());


            var B1 = new RuntimeIshtarInterface(type, new [] { A1 }, _module);

            var FooB1 = B1.DefineMethod("FooB1", TYPE_VOID.AsRuntimeClass(), MethodFlags.Public);

            var FieldB1 = B1.DefineField("FieldB1", FieldFlags.Public, TYPE_I4.AsRuntimeClass());


            var C1 = new RuntimeIshtarInterface(type, new [] {  B1, A2 }, _module);

            var FooC1 = C1.DefineMethod("FooC1", TYPE_VOID.AsRuntimeClass(), MethodFlags.Public);

            var FieldC1 = C1.DefineField("FieldC1", FieldFlags.Public, TYPE_I4.AsRuntimeClass());

            C1.init_vtable();

            var offset_FooA1 = C1.get_vtable_offset(FooA1);
            var offset_FieldA1 = C1.get_vtable_offset(FieldA1);

            Assert.Equal((nint)A1.vtable[FooA1.vtable_offset], (nint)C1.vtable[offset_FooA1]);
            Assert.Equal((nint)A1.vtable[FieldA1.vtable_offset], (nint)C1.vtable[offset_FieldA1]);
        }
    }
}
