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

        public ushort GrantModuleID()
        {
            Interlocked.MemoryBarrier();
            lock (_guarder.TokenInterlockerGuard)
            {
                return Increment(ref _vault.LastModuleID);
            }
        }

        public ushort GrantClassID()
        {
            Interlocked.MemoryBarrier();
            lock (_guarder.TokenInterlockerGuard)
            {
                return Increment(ref _vault.LastClassID);
            }
        }

        private static unsafe ushort Increment(ref ushort location)
        {
            fixed (ushort* ptr = &location)
            {
                return (ushort)Interlocked.Increment(ref *(int*)ptr);
            }
        }
    }
}
