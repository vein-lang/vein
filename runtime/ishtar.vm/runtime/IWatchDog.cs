namespace ishtar
{
    public unsafe interface IWatchDog
    {
        void FastFail(WNE type, string msg, CallFrame* frame = null);
        void ValidateLastError();
    }
}
