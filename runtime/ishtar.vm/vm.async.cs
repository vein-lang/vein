namespace ishtar;

using io;
using runtime.gc;
using vein.runtime;
using static vein.runtime.VeinTypeCode;
using static WNE;

public unsafe partial struct VirtualMachine
{
    /// <summary>
    /// Capture the current interpreter state into a single contiguous allocation.
    /// Layout: [SuspendedFrame | stackval[stackDepth] | stackval[localsCount]]
    /// </summary>
    internal SuspendedFrame* CaptureFrame(
        RuntimeIshtarMethod* method,
        uint* ip,
        stackval* spStart,
        int stackDepth,
        stackval* localsPtr,
        int localsCount,
        CallFrame* parent,
        stackval* args,
        int maxStack,
        IshtarAsyncJob* ownerJob,
        IshtarAsyncJob* awaitedJob)
    {
        var headerSize = (uint)sizeof(SuspendedFrame);
        var evalSize = (uint)(stackDepth * sizeof(stackval));
        var localsSize = (uint)(localsCount * sizeof(stackval));
        var totalSize = headerSize + evalSize + localsSize;

        var block = (byte*)IshtarGC.AllocateImmortal(totalSize, @ref);
        var frame = (SuspendedFrame*)block;
        *frame = default;

        frame->method = method;
        frame->savedIP = ip;
        frame->parent = parent;
        frame->args = args;
        frame->maxStack = maxStack;
        frame->ownerJob = ownerJob;
        frame->awaitedJob = awaitedJob;
        frame->vm = @ref;
        frame->next = null;
        frame->evalStackDepth = stackDepth;
        frame->localsCount = localsCount;

        // eval stack snapshot follows the header
        if (stackDepth > 0)
        {
            frame->evalStack = (stackval*)(block + headerSize);
            Buffer.MemoryCopy(spStart, frame->evalStack, evalSize, evalSize);
        }

        // locals snapshot follows the eval stack
        if (localsCount > 0)
        {
            frame->locals = (stackval*)(block + headerSize + evalSize);
            Buffer.MemoryCopy(localsPtr, frame->locals, localsSize, localsSize);
        }

        return frame;
    }

    /// <summary>
    /// Free a suspended frame (single allocation, no sub-pointers to free).
    /// </summary>
    internal static void FreeSuspendedFrame(SuspendedFrame* frame)
    {
        IshtarGC.FreeImmortal(frame);
    }

    /// <summary>
    /// Resume execution of a suspended async frame.
    /// Called by the thread pool worker when the awaited Job has completed.
    /// </summary>
    public void exec_method_resume(SuspendedFrame* suspended)
    {
        if (HasFaulted())
        {
            FreeSuspendedFrame(suspended);
            return;
        }

        var awaitedJob = suspended->awaitedJob;
        var ownerJob = suspended->ownerJob;

        if (awaitedJob->state == JobState.Faulted)
        {
            if (ownerJob != null)
                ownerJob->SetException(awaitedJob->exception);
            FreeSuspendedFrame(suspended);
            return;
        }

        var resumeFrame = CallFrame.Create(suspended->method, suspended->parent);
        resumeFrame->args = suspended->args;
        resumeFrame->asyncJob = ownerJob;
        resumeFrame->resumeState = suspended;
        resumeFrame->awaitResult = awaitedJob->result;

        exec_method(resumeFrame);

        if (!resumeFrame->exception.IsDefault() && ownerJob != null && ownerJob->state == JobState.Pending)
            ownerJob->SetException(resumeFrame->exception);

        resumeFrame->Dispose();
    }

    /// <summary>
    /// Get the native IshtarAsyncJob* from a Job&lt;T&gt; IshtarObject.
    /// </summary>
    internal IshtarAsyncJob* GetJobFromObject(IshtarObject* obj, CallFrame* frame)
    {
        if (obj == null)
            return null;
        
        var field = obj->clazz->FindField("_nativeHandle");
        if (field == null)
            return null;
        
        return (IshtarAsyncJob*)obj->vtable[field->vtable_offset];
    }

    /// <summary>
    /// Set the native IshtarAsyncJob* on a Job&lt;T&gt; IshtarObject.
    /// </summary>
    internal void SetJobOnObject(IshtarObject* obj, IshtarAsyncJob* job, CallFrame* frame)
    {
        var field = obj->clazz->FindField("_nativeHandle");
        if (field == null)
        {
            FastFail(STATE_CORRUPT, "Job object missing _nativeHandle field", frame);
            return;
        }
        obj->vtable[field->vtable_offset] = job;
    }
}
