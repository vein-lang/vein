namespace vein
{
    using compilation;
    using ishtar;

    public class AssemblyResolver : ModuleResolverBase
    {
        private readonly Compiler _c;
        public AssemblyResolver(Compiler c) => _c = c;

        protected override void debug(string s) => Log.Info(s);
    }
}
