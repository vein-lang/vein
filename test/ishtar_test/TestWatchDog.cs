namespace ishtar_test
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using ishtar;

    [ExcludeFromCodeCoverage]
    public class WatchDogEffluentException : Exception
    {
        public WatchDogEffluentException(NativeException exp)
            : base($"Ishtar internal error was thrown. [{exp.code}] '{exp.msg}'")
        { }
    }
    [ExcludeFromCodeCoverage]
    public unsafe class TestWatchDog : IWatchDog
    {
        private static readonly object guarder = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IWatchDog.FastFail(WNE type, string msg, CallFrame* frame)
        {
            lock (guarder)
            {
                var result = new NativeException {code = type, msg = msg, frame = frame};
                throw new WatchDogEffluentException(result);
            }
        }

        void IWatchDog.ValidateLastError()
        {
        }
    }
}
