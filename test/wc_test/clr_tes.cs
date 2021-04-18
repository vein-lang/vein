namespace wc_test
{
    using wave.clr.emit;
    using wave.runtime;
    using Xunit;

    public class clr_test
    {
        [Fact]
        public void F1()
        {
            var m = new WaveModuleBuilder("foo");

            var clazz = m.DefineClass("wa/zoo", ClassFlags.Public);

            var method = clazz.DefineMethod("Asd", WaveTypeCode.TYPE_VOID.AsType(), MethodFlags.Public);
        }
    }
}