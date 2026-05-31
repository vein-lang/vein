namespace ishtar.io;

using collections;
using ishtar.runtime.io;
using runtime;
using runtime.gc;
using libuv;
using vein.runtime;
using static libuv.LibUV;
using static VirtualMachine;

/// <summary>
/// The async runtime scheduler backed by libuv.
/// 
/// Architecture:
/// - Owns a dedicated libuv event loop running on its own thread.
/// - I/O completions (timers, tcp, fs) resolve IshtarJobs on the event loop thread.
/// - When a Job resolves, its continuations (SuspendedFrames) are dispatched
///   to the thread pool for resumption.
/// - Cross-thread wake-up via uv_async_send when new work is submitted.
/// </summary>
[CTypeExport("ishtar_job_scheduler_t")]
public unsafe struct JobScheduler : IDisposable
{
    private nint loop;
    private void* asyncWakeup;
    private VirtualMachine* _vm;
    private NativeQueue<SuspendedFrame>* _resumptionQueue;
    private uv_mutex_t* _queueMutex;
    private bool _loopRunning;

    public nint Loop => loop;

    public static JobScheduler* Create(VirtualMachine* vm)
    {
        using var tag = Profiler.Begin("vm:job_scheduler:create");

        var scheduler = IshtarGC.AllocateImmortal<JobScheduler>(vm);
        *scheduler = default;
        scheduler->_vm = vm;

        // Create a new loop (not default — we own this one)
        scheduler->loop = uv_loop_new();

        // Set scheduler pointer as loop data for callbacks
        uv_loop_set_data(scheduler->loop, scheduler);

        // Create resumption queue
        scheduler->_resumptionQueue = IshtarGC.AllocateQueue<SuspendedFrame>(scheduler);
        scheduler->_queueMutex = IshtarGC.AllocateImmortal<uv_mutex_t>(scheduler);
        uv_mutex_init(scheduler->_queueMutex);

        // Create async handle for cross-thread wake-up
        var asyncMem = (nint)IshtarGC.AllocateAtomicImmortal(128);
        Assert(uv_async_init(scheduler->loop, asyncMem, OnAsyncWakeup) == 0,
            WNE.THREAD_STATE_CORRUPTED, "job_scheduler: failed to init async handle", vm->Frames->EntryPoint);
        ((uv_async_t*)asyncMem)->data = scheduler;
        scheduler->asyncWakeup = (void*)asyncMem;

        return scheduler;
    }

    /// <summary>
    /// Queue a suspended frame for resumption on a worker thread.
    /// Called from any thread (I/O callbacks, job resolution, etc.).
    /// Wakes up the event loop to process the queue.
    /// </summary>
    public void QueueResumption(SuspendedFrame* frame)
    {
        uv_mutex_lock(_queueMutex);
        _resumptionQueue->Enqueue(frame);
        uv_mutex_unlock(_queueMutex);

        // Wake event loop so it dispatches to thread pool
        uv_async_send((nint)asyncWakeup);
    }

    /// <summary>
    /// Callback fired on the event loop thread when async_send is called.
    /// Drains the resumption queue and dispatches frames to the thread pool.
    /// </summary>
    public static void OnAsyncWakeup(nint handle)
    {
        var asyncHandle = (uv_async_t*)handle;
        var scheduler = (JobScheduler*)asyncHandle->data;
        scheduler->DrainResumptionQueue();
    }

    private void DrainResumptionQueue()
    {
        uv_mutex_lock(_queueMutex);
        
        while (_resumptionQueue->TryDequeue(out var frame))
        {
            // Dispatch to thread pool for actual execution
            _vm->thread_pool->QueueResumption(frame);
        }
        
        uv_mutex_unlock(_queueMutex);
    }

    /// <summary>
    /// Run the event loop (called on the dedicated scheduler thread).
    /// </summary>
    public void Run() => uv_run(loop, uv_run_mode.UV_RUN_DEFAULT);

    /// <summary>
    /// Stop the event loop.
    /// </summary>
    public void Stop() => uv_stop(loop);

    /// <summary>
    /// Start the scheduler on its own thread.
    /// </summary>
    public void StartThread(VirtualMachine* vm)
    {
        _loopRunning = true;

        static void execute_scheduler(IshtarRawThread* thread)
        {
            GlobalPrintln("job_scheduler:start");

            var vm = thread->vm;
            var gcInfo = new GC_stack_base();
            vm->gc->get_stack_base(&gcInfo);
            vm->gc->register_thread(&gcInfo);

            vm->job_scheduler->Run();

            vm->gc->unregister_thread();
            GlobalPrintln("job_scheduler:end");
        }

        vm->threading
            .CreateRawThread(&execute_scheduler, null, "IshtarJobScheduler::loop");
    }

    public void Dispose()
    {
        if (_loopRunning)
        {
            Stop();

            // Close the async handle so the loop has no active handles
            uv_close((nint)asyncWakeup, null);

            // Drain pending callbacks (processes the close callback)
            uv_run(loop, uv_run_mode.UV_RUN_NOWAIT);
        }

        uv_loop_close(loop);
        uv_loop_delete(loop);

        // Free the async handle allocation
        IshtarGC.FreeAtomicImmortal(asyncWakeup);

        // Free the resumption queue and mutex
        IshtarGC.FreeQueue(_resumptionQueue);
        uv_mutex_destroy(_queueMutex);
        IshtarGC.FreeImmortal(_queueMutex);
    }
}
