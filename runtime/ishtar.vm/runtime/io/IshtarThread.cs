namespace ishtar.io;

using collections;
using static libuv.LibUV;

public readonly unsafe struct IshtarThread(
    uv_thread_t threadId,
    CallFrame* frame,
    IshtarThreadContext* ctx) : IEq<IshtarThread>
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
        => ctx->Status = IshtarThreadStatus.EXITED;

    public void join()
    {
        var err = uv_thread_join(threadId);
        VirtualMachine.Assert(err == 0, WNE.THREAD_STATE_CORRUPTED,
            $"uv_thread_join returned error status, code: {err}");
    }

    public static bool Eq(IshtarThread* p1, IshtarThread* p2) => p1->threadId.handle.Equals(p2->threadId.handle);
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


public enum IshtarJobStatus
{
    CREATED,
    RUNNING,
    PAUSED,
    CANCELED,
    EXITED
}

public unsafe struct IshtarJobContext(
    uv_work_t* jobId,
    uv_sem_t locker
)
{
    public IshtarJobStatus Status = IshtarJobStatus.CREATED;
    public uv_work_t* JobId = jobId;
    public uv_sem_t Locker = locker;
}

public readonly unsafe struct IshtarJob(uv_work_t* workerId, CallFrame* frame, IshtarJobContext* ctx) : IEq<IshtarJob>
{
    public readonly IshtarJobContext* ctx = ctx;
    public readonly uv_work_t* workerId = workerId;
    public readonly CallFrame* callFrame = frame;

    public void start()
    {
        VirtualMachine.Assert(ctx->Status == IshtarJobStatus.CREATED, WNE.THREAD_STATE_CORRUPTED,
            "trying start worker with out of CREATED status");
        ctx->Status = IshtarJobStatus.RUNNING;
        uv_sem_post(ref ctx->Locker);
    }

    public void complete()
        => ctx->Status = IshtarJobStatus.EXITED;

    public void cancel()
    {
        ctx->Status = IshtarJobStatus.CANCELED;
        var err = uv_cancel(workerId);
        VirtualMachine.Assert(err == 0, WNE.THREAD_STATE_CORRUPTED,
            $"uv_cancel work_t returned error status, code: {err}");
    }

    public void wait_until_complete_or_cancel()
    {
        begin:
        if (ctx->Status is IshtarJobStatus.EXITED)
            return;
        if (ctx->Status is IshtarJobStatus.CANCELED)
            return;

        uv_sleep(1);
        goto begin;
    }


    public static bool Eq(IshtarJob* p1, IshtarJob* p2) => p1->workerId->handle.Equals(p2->workerId->handle);
}
