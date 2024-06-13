namespace ishtar
{
    using System.Threading;
    public class TokenInterlocker
    {
        private readonly AppVault _vault;
        private readonly AppVaultSync _guarder;

        public TokenInterlocker(AppVault vault, AppVaultSync guarder)
        {
            _vault = vault;
            _guarder = guarder;
        }

        public uint GrantModuleID()
        {
            Interlocked.MemoryBarrier();
            lock (_guarder.TokenInterlockerGuard)
            {
                return Increment(ref _vault.LastModuleID);
            }
        }

        public uint GrantClassID()
        {
            Interlocked.MemoryBarrier();
            lock (_guarder.TokenInterlockerGuard)
            {
                return Increment(ref _vault.LastClassID);
            }
        }

        private static unsafe uint Increment(ref uint location)
        {
            return Interlocked.Increment(ref location);
        }
    }
}
