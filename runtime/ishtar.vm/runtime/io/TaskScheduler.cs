namespace ishtar.io;
using collections;
using runtime;
using runtime.gc;
using System.Threading;
using libuv;
using vein.runtime;
using static VirtualMachine;
using static libuv.LibUV;

[CTypeExport("ishtar_scheduler_t")]
public unsafe struct TaskScheduler(NativeQueue<IshtarTask>* queue, VirtualMachine* vm) : IDisposable
{
    private ulong task_index;
    private void* async_header;
    private nint loop;
    private readonly NativeQueue<IshtarTask>* _queue = queue;
    private readonly VirtualMachine* _vm = vm; 


    public nint getLoop() => loop;


    public static TaskScheduler* Create(VirtualMachine* vm)
    {
        using var tag = Profiler.Begin("vm:scheduler:create");

        var scheduler = IshtarGC.AllocateImmortal<TaskScheduler>(vm);
        var queue = IshtarGC.AllocateQueue<IshtarTask>(scheduler);
        *scheduler = new TaskScheduler(queue, vm);

        var asyncHeader = IshtarGC.AllocateImmortal<nint>(vm);
        scheduler->loop = uv_default_loop();
        Assert(uv_async_init(scheduler->loop, (nint)asyncHeader, on_async) == 0,
            WNE.THREAD_STATE_CORRUPTED, "scheduler has failed create async io");
        ((uv_async_t*)asyncHeader)->data = scheduler;
        scheduler->async_header = asyncHeader;
        scheduler->task_index = 0;

        return scheduler;
    }

    public static void Free(TaskScheduler* scheduler)
    {
        // TODO
    }

    public struct ishtar_worker_t
    {
        public VirtualMachine* vm;
        public CallFrame* method;
    }

    public static void on_async(nint handler)
    {
        var bw = (uv_async_t*)handler;
        var taskScheduler = (TaskScheduler*)bw->data;
        var queue = taskScheduler->_queue;
        while (queue->TryDequeue(out var task))
        {
            task->Frame->vm->exec_method(task->Frame);
            uv_sem_post(ref task->Data->semaphore);
        }
    }
    public void execute_method(CallFrame* frame)
    {
        if ((frame->method->Flags & MethodFlags.Async) != 0)
            doAsyncExecute(frame);
        else
            doExecute(frame);
    }

    private void doExecute(CallFrame* frame)
        => frame->vm->exec_method(frame);

    private void doAsyncExecute(CallFrame* frame)
    {
        // TODO remove using interlocked
        var taskIdx = Interlocked.Increment(ref task_index);
        var task = IshtarGC.AllocateImmortal<IshtarTask>(frame);

        *task = new IshtarTask(frame, taskIdx, TaskPriority.NORMAL);

        uv_sem_init(out task->Data->semaphore, 0);

        _queue->Enqueue(task);

        uv_async_send((nint)async_header);

        uv_sem_wait(ref task->Data->semaphore);
        uv_sem_destroy(ref task->Data->semaphore);
        task->Dispose();
        IshtarGC.FreeImmortal(task);
    }

    public void Dispose()
    {
        stop();
        IshtarGC.FreeQueue(_queue);
    }

    public void runOnce() => uv_run(loop, uv_run_mode.UV_RUN_NOWAIT);
    public void run() => uv_run(loop, uv_run_mode.UV_RUN_DEFAULT);
    public void stop() => uv_stop(loop);


    public void start_threading(VirtualMachine* vm)
    {
        static void execute_scheduler(IshtarRawThread* thread)
        {
            GlobalPrintln("execute_scheduler:start");

            var vm = thread->vm;

            var gcInfo = new GC_stack_base();

            vm->gc->get_stack_base(&gcInfo);

            vm->gc->register_thread(&gcInfo);

            vm->task_scheduler->run();

            vm->gc->unregister_thread();
            GlobalPrintln("execute_scheduler:end");
        }

        vm->threading
            .CreateRawThread(&execute_scheduler, null, "IshtarScheduler::&execute");
    }
}
