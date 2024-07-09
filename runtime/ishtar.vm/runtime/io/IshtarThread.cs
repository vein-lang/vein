namespace ishtar.io;

using collections;
using static libuv.LibUV;

public readonly unsafe struct IshtarThread(
    uv_thread_t threadId,
    CallFrame* frame,
    IshtarThreadContext* ctx)
{
    public readonly IshtarThreadContext* ctx = ctx;
    public readonly uv_thread_t threadId = threadId;
    public readonly CallFrame* callFrame = frame;

    public void start()
    {
        VirtualMachine.Assert(ctx->Status == IshtarThreadStatus.CREATED, WNE.THREAD_STATE_CORRUPTED,
            "trying start thread with out of CREATED status");
        ctx->Status = IshtarThreadStatus.RUNNING;
        uv_sem_post(ref ctx->Locker);
    }

    public void complete()
    {
    }
}

public struct IshtarThreadContext(
    uv_thread_t threadId,
    uv_sem_t locker
)
{
    public IshtarThreadStatus Status = IshtarThreadStatus.CREATED;
    public uv_thread_t ThreadId = threadId;
    public uv_sem_t Locker = locker;
}

public enum IshtarThreadStatus
{
    CREATED,
    RUNNING,
    PAUSED,
    EXITED
}
