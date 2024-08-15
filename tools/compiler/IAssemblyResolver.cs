namespace vein
{
    using compilation;

    public class AssemblyResolver : ModuleResolverBase
    {
        private readonly CompilationTarget _target;

        public AssemblyResolver(CompilationTarget target) => _target = target;
        public AssemblyResolver() { }
        protected override void debug(string s) => Log.Info(s, _target);
    }
}
