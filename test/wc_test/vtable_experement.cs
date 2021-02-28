namespace wc_test
{
    using System;
    using System.Threading;
    using Xunit;
    using Xunit.Abstractions;

    public unsafe class vtable_experement
    {
        private static ITestOutputHelper console;

        public vtable_experement(ITestOutputHelper testOutputHelper) => console = testOutputHelper;

        [Fact]
        public void F1()
        {
            var f = new delegate*<void>[] { &Foo, &Bar };

            var overrides = new delegate*<void>[f.Length * 2];


            for (var i = 0; i < f.Length; ++i)
            {
                overrides[i * 2] = f[i];
                if (f[i] != (delegate*<void>) &Foo)
                        continue;
                Thread.MemoryBarrier();
                var tmp = overrides[(i * 2)];
                overrides[i * 2 + 1] = tmp;
                overrides[i * 2] = &FooOverride;
            }
            
            for (var i = 0; i != f.Length; i++) 
                overrides[i * 2]();
        }
        
        
        
        public static void Foo()
        {
            console.WriteLine("Foo");
        }
        public static void Bar()
        {
            console.WriteLine("Bar");
        }
        public static void FooOverride()
        {
            console.WriteLine("FooOverride");
        }
    }
}