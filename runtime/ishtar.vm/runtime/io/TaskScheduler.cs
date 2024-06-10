namespace ishtar.io;

using collections;
using runtime;
using runtime.gc;
using vein.runtime;
using vm.libuv;

public readonly unsafe struct TaskScheduler(
    nint loopHandle,
    nint* asyncHandle,
    NativeQueue<IshtarTask>* queue) : IDisposable
{
    private readonly Data* data = IshtarGC.AllocateImmortal<Data>();

    public static TaskScheduler* Create()
    {
        var scheduler = IshtarGC.AllocateImmortal<TaskScheduler>();
        var loop = LibUV.uv_default_loop();
        var queue = IshtarGC.AllocateQueue<IshtarTask>();
        var asyncHandle = IshtarGC.AllocateImmortal<nint>();

        *scheduler = new TaskScheduler(loop, asyncHandle, queue);

        LibUV.uv_async_init(loop, (nint)asyncHandle, scheduler->onAsync);

        return scheduler;
    }

    public static void Free(TaskScheduler* scheduler)
    {
        // TODO
    }

    public void onAsync(nint handler)
    {
        while (queue->TryDequeue(out var task))
        {
            task->Frame->vm.exec_method(task->Frame);
            LibUV.uv_sem_post(ref task->Data->semaphore);
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
        => frame->vm.exec_method(frame);

    private void doAsyncExecute(CallFrame* frame)
    {
        // TODO remove using interlocked
        var taskIdx = Interlocked.Increment(ref data->task_index);
        var task = IshtarGC.AllocateImmortal<IshtarTask>();

        *task = new IshtarTask(frame, taskIdx);

        LibUV.uv_sem_init(out task->Data->semaphore, 0);

        queue->Enqueue(task);

        LibUV.uv_async_send((nint)asyncHandle);

        LibUV.uv_sem_wait(ref task->Data->semaphore);
        LibUV.uv_sem_destroy(ref task->Data->semaphore);
        task->Dispose();
        IshtarGC.FreeImmortal(task);
    }

    public void Dispose() => IshtarGC.FreeImmortal(data);

    public void run() => LibUV.uv_run(loopHandle, LibUV.uv_run_mode.UV_RUN_DEFAULT);
    public void stop() => LibUV.uv_stop(loopHandle);


    public void start_threading(RuntimeIshtarModule* entryModule)
    {
        static void execute_scheduler(nint args)
        {
            var thread = (IshtarRawThread*)args;
            var vm = thread->MainModule->vm;

            var gcInfo = new GC_stack_base();

            vm.GC.get_stack_base(&gcInfo);

            vm.GC.register_thread(&gcInfo);

            vm.task_scheduler->run();

            vm.GC.unregister_thread();
        }

        entryModule->vm.threading.CreateRawThread(entryModule, &execute_scheduler);
    }

    private struct Data
    {
        public ulong task_index;
    }
}