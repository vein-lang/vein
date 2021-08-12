namespace ishtar_test
{
    using System;
    using ishtar;
    using mana.runtime;
    using Xunit;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    public unsafe class OverrideTest : IshtarTestBase
    {
        [Fact]
        public void TestValidCall()
        {
            var module = new RuntimeIshtarModule(AppVault.CurrentVault, _module.Name);

            var b1 = new RuntimeIshtarClass("tst%global::foo/bar1", ManaTypeCode.TYPE_OBJECT.AsRuntimeClass(), module);

            var m1 = b1.DefineMethod("soq", ManaTypeCode.TYPE_VOID.AsRuntimeClass(), MethodFlags.Public | MethodFlags.Virtual);

            m1.PIInfo = PInvokeInfo.New(((delegate*<void>)&Foo1));

            var b2 = new RuntimeIshtarClass("tst%global::foo/bar2", b1, module);

            var m2 = b2.DefineMethod("soq", ManaTypeCode.TYPE_VOID.AsRuntimeClass(), MethodFlags.Public | MethodFlags.Override);

            m2.PIInfo = PInvokeInfo.New(((delegate*<void>)&Foo2));

            b2.init_vtable();


            ((delegate*<void>)b2.Method["soq()"].PIInfo.Addr)();



            var result = IshtarGC.AllocObject(b2);

            var pointer = result->vtable[m2.vtable_offset];

            var d2 = IshtarUnsafe.AsRef<RuntimeIshtarMethod>(pointer);
            ((delegate*<void>)d2.PIInfo.Addr)();
        }


        [Fact]
        public void TestNotValidCall()
        {
            //var module = new RuntimeIshtarModule(AppVault.CurrentVault, _module.Name);

            //var b1 = new RuntimeIshtarClass("tst%global::foo/bar1", ManaTypeCode.TYPE_OBJECT.AsRuntimeClass(), module);

            //var m1 = b1.DefineMethod("soq", ManaTypeCode.TYPE_VOID.AsRuntimeClass(), MethodFlags.Public | MethodFlags.Virtual);

            //m1.PIInfo = PInvokeInfo.New(((delegate*<void>)&Foo1));

            //var b2 = new RuntimeIshtarClass("tst%global::foo/bar2", b1, module);

            //var m2 = b2.DefineMethod("soq", ManaTypeCode.TYPE_VOID.AsRuntimeClass(), MethodFlags.Public | MethodFlags.Override);

            //m2.PIInfo = PInvokeInfo.New(((delegate*<void>)&Foo2));

            //b2.init_vtable();

            //var result1 = IshtarGC.AllocObject(b1);
            //var result2 = IshtarGC.AllocObject(b2);

            //var offset = b1.Method["soq()"].vtable_offset;

            //var w1 = b1.dvtable.vtable[offset];
            //var w2 = b2.dvtable.vtable[offset];

            //var p1 = result1->vtable[offset];
            //var p2 = result2->vtable[offset];


            //var cp1 = b1.vtable[offset];
            //var cp2 = b2.vtable[offset];

            //var d1 = IshtarUnsafe.AsRef<RuntimeIshtarMethod>(p1);
            //var d2 = IshtarUnsafe.AsRef<RuntimeIshtarMethod>(p2);

            //Assert.Equal((nint)p1, (nint)cp1);
            //Assert.Equal((nint)p2, (nint)cp2);
            //Assert.NotNull(d1);
            //Assert.NotNull(d2);
        }

        public static void Foo1() => Assert.False(true);

        public static void Foo2() => Console.WriteLine("Foo2");
    }
}
