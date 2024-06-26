namespace ishtar;

using vein.runtime;

public static unsafe class B_Threading
{
    public static stackval createThread(stackval* args, int argsSize)
    {
        var frame = (CallFrame*)args[0].data.p;
        var targetFrame = (CallFrame*)args[1].data.p;

        frame->vm.threading.CreateThread(targetFrame);

        return default;
    }

    public static void InitTable(ForeignFunctionInterface ffi)
    {
        ffi.Add("_createThread", MethodFlags.Static, VeinTypeCode.TYPE_VOID)
            ->AsNative(&createThread);
    }
}
