namespace wc_test
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;
    using NUnit.Framework;

    public unsafe class vtable_experement
    {
        [Test]
        public void F1()
        {
            var f = new delegate*<void>[] { &Foo, &Bar };

            var overrides = new delegate*<void>[f.Length * 2];


            for (var i = 0; i < f.Length; ++i)
            {
                overrides[i * 2] = f[i];
                if (f[i] != (delegate*<void>)&Foo)
                    continue;
                Thread.MemoryBarrier();
                var tmp = overrides[(i * 2)];
                overrides[i * 2 + 1] = tmp;
                overrides[i * 2] = &FooOverride;
            }

            for (var i = 0; i != f.Length; i++)
                overrides[i * 2]();
        }
        public struct SSS
        {
            public int i;
        }

        [Test]
        public unsafe void F3()
        {
            var vtable = (void**)Marshal.AllocHGlobal(new IntPtr(sizeof(void*) * 12));
            var i = new SSS {i = 4};
            vtable[0] = &i;
            i.i = 5;
            vtable[1] = &i;

            var b = *(SSS*) vtable[0];
        }

        public class XID
        {
            public string Soo = "test";
        }
        [Test]
        public unsafe void F4()
        {
            var x = new XID();

            var pointer = Unsafe.AsPointer(ref x);


            var x_unpacked = Unsafe.AsRef<XID>(pointer);

        }


        public static void Foo()
        {
            Console.WriteLine("Foo");
        }
        public static void Bar()
        {
            Console.WriteLine("Bar");
        }
        public static void FooOverride()
        {
            Console.WriteLine("FooOverride");
        }
    }
}
