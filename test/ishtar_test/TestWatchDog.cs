namespace ishtar_test
{
    using System;
    using System.Threading;
    using ishtar;

    public class WatchDogEffluentException : Exception
    {
        public WatchDogEffluentException(NativeException exp)
            : base($"native exception was thrown. [{exp.code}] '{exp.msg}'")
        { }
    }
    public class TestWatchDog : IWatchDog
    {
        private static readonly object guarder = new();
        void IWatchDog.FastFail(WNE type, string msg, CallFrame frame)
        {
            lock (guarder)
            {
                var result = new NativeException { code = type, msg = msg, frame = frame };
                throw new WatchDogEffluentException(result);
            }
        }

        void IWatchDog.ValidateLastError()
        {
        }
    }
}