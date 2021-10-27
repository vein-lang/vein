namespace vein
{
    using compilation;
    using ishtar;

    public class AssemblyResolver : ModuleResolverBase
    {
        protected override void debug(string s) => Log.Info(s);
    }
}
