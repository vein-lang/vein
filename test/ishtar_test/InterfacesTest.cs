namespace ishtar_test
{
    using System;
    using ishtar;
    using vein.runtime;
    using NUnit.Framework;

    public unsafe class InterfacesTest : IshtarTestBase
    {


        [Test]
        [Parallelizable(ParallelScope.None)]
        public void ValidVTableInitialization()
        {
            var module = new RuntimeIshtarModule(GetVM().Vault, _module.Name);

            var IFoo1 = new RuntimeIshtarClass("tst%global::foo/IFoo1", T.OBJECT, module);
            var IFoo2 = new RuntimeIshtarClass("tst%global::foo/IFoo2", T.OBJECT, module);


            IFoo1.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            IFoo2.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;

            IFoo1.DefineMethod("doodoo", T.VOID,
                MethodFlags.Abstract | MethodFlags.Public);

            IFoo2.DefineMethod("moomoo", T.VOID,
                MethodFlags.Abstract | MethodFlags.Public);

            var Zoo1 = new RuntimeIshtarClass("tst%global::foo/Zoo1", new []
            {
                T.OBJECT,
                IFoo1,
                IFoo2
            }, module);

            var method1 =
                Zoo1.DefineMethod("doodoo", T.VOID, MethodFlags.Override | MethodFlags.Public);
            var method2 =
                Zoo1.DefineMethod("moomoo", T.VOID, MethodFlags.Override | MethodFlags.Public);


            method1.PIInfo = (delegate*<void>)&Foo1;
            method2.PIInfo = (delegate*<void>)&Foo2;


            Assert.DoesNotThrow(() => Zoo1.init_vtable(GetVM()));
            Assert.DoesNotThrow(() => ((delegate*<void>)Zoo1.Method["doodoo()"].PIInfo.Addr)());
            Assert.DoesNotThrow(() => ((delegate*<void>)Zoo1.Method["moomoo()"].PIInfo.Addr)());
        }

        public static void Foo1() => Console.WriteLine("Foo1");

        public static void Foo2() => Console.WriteLine("Foo2");
    }
}
