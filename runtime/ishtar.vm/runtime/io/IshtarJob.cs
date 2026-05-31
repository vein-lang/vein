namespace ishtar.io;

using collections;
using runtime.gc;
using libuv;
using static libuv.LibUV;

/// <summary>
/// Native backing structure for async Job&lt;T&gt; in the VM.
/// Represents the state of an asynchronous operation.
/// GC-managed (allocated via Boehm GC).
/// </summary>
[CTypeExport("ishtar_async_job_t")]
public unsafe struct IshtarAsyncJob : IEq<IshtarAsyncJob>
{
    public JobState state;
    public stackval result;
    public CallFrameException exception;
    
    /// <summary>
    /// Linked list of suspended frames waiting for this job to complete.
    /// When the job resolves, all continuations are dispatched to the thread pool.
    /// </summary>
    public SuspendedFrame* continuations;
    
    /// <summary>
    /// Mutex protecting state transitions and continuation registration.
    /// Ensures atomicity between "check if completed" and "register continuation".
    /// </summary>
    public uv_mutex_t* @lock;
    
    /// <summary>
    /// Back-reference to the IshtarObject wrapping this job (the user-visible Job&lt;T&gt; instance).
    /// </summary>
    public IshtarObject* owner;

    /// <summary>
    /// Pointer to the VM that owns this job (for scheduling continuations).
    /// </summary>
    public VirtualMachine* vm;

    public static IshtarAsyncJob* Create(VirtualMachine* vm, IshtarObject* ownerObject)
    {
        var job = IshtarGC.AllocateImmortal<IshtarAsyncJob>(vm);
        *job = default;
        job->state = JobState.Pending;
        job->continuations = null;
        job->owner = ownerObject;
        job->vm = vm;
        job->@lock = IshtarGC.AllocateImmortal<uv_mutex_t>(job);
        uv_mutex_init(job->@lock);
        return job;
    }

    /// <summary>
    /// Create a job that's not attached to any IshtarObject yet (for internal use).
    /// </summary>
    public static IshtarAsyncJob* Create(VirtualMachine* vm)
    {
        var job = IshtarGC.AllocateImmortal<IshtarAsyncJob>(vm);
        *job = default;
        job->state = JobState.Pending;
        job->continuations = null;
        job->owner = null;
        job->vm = vm;
        job->@lock = IshtarGC.AllocateImmortal<uv_mutex_t>(job);
        uv_mutex_init(job->@lock);
        return job;
    }

    /// <summary>
    /// Resolve the job with a successful result.
    /// Dispatches all registered continuations to the thread pool.
    /// </summary>
    public void SetResult(stackval value)
    {
        uv_mutex_lock(@lock);
        
        if (state != JobState.Pending)
        {
            uv_mutex_unlock(@lock);
            return; // already resolved or faulted — ignore duplicate resolution
        }
        
        result = value;
        state = JobState.Completed;
        
        // Harvest continuations before unlocking
        var cont = continuations;
        continuations = null;
        
        uv_mutex_unlock(@lock);
        
        // Dispatch all waiting frames to thread pool for resumption
        DispatchContinuations(cont);
    }

    /// <summary>
    /// Resolve the job with a void result (for Job without type parameter).
    /// </summary>
    public void SetCompleted()
    {
        var voidResult = new stackval();
        voidResult.type = vein.runtime.VeinTypeCode.TYPE_VOID;
        SetResult(voidResult);
    }

    /// <summary>
    /// Fault the job with an exception.
    /// Dispatches all registered continuations (they will re-throw).
    /// </summary>
    public void SetException(CallFrameException ex)
    {
        uv_mutex_lock(@lock);
        
        if (state != JobState.Pending)
        {
            uv_mutex_unlock(@lock);
            return;
        }
        
        exception = ex;
        state = JobState.Faulted;
        
        var cont = continuations;
        continuations = null;
        
        uv_mutex_unlock(@lock);
        
        DispatchContinuations(cont);
    }

    /// <summary>
    /// Register a suspended frame as a continuation.
    /// If the job is already completed, immediately dispatches the frame.
    /// Returns true if the frame was suspended (job is pending).
    /// Returns false if the job was already complete (caller should continue synchronously).
    /// </summary>
    public bool TryRegisterContinuation(SuspendedFrame* frame)
    {
        uv_mutex_lock(@lock);
        
        if (state != JobState.Pending)
        {
            // Job already done — caller can continue synchronously
            uv_mutex_unlock(@lock);
            return false;
        }
        
        // Prepend to continuation linked list
        frame->next = continuations;
        continuations = frame;
        
        uv_mutex_unlock(@lock);
        return true; // frame is now suspended
    }

    private void DispatchContinuations(SuspendedFrame* cont)
    {
        while (cont != null)
        {
            var next = cont->next;
            vm->job_scheduler->QueueResumption(cont);
            cont = next;
        }
    }

    public void Dispose()
    {
        uv_mutex_destroy(@lock);
        IshtarGC.FreeImmortal(@lock);
    }

    public static bool Eq(IshtarAsyncJob* p1, IshtarAsyncJob* p2) => p1 == p2;
}

public enum JobState : byte
{
    Pending = 0,
    Completed = 1,
    Faulted = 2,
    Canceled = 3
}
