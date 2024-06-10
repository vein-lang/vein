namespace ishtar.io;

using static vm.libuv.LibUV;

public readonly unsafe struct IshtarThread(
    uv_thread_t threadId,
    CallFrame* frame)
{
    public readonly uv_thread_t threadId = threadId;
    public readonly CallFrame* callFrame = frame;
}
