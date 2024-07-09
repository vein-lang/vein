namespace ishtar.io;

using collections;
using runtime;
using runtime.gc;
using libuv;

public unsafe struct IshtarThreading
{
    public IshtarRawThread* CreateRawThread(RuntimeIshtarModule* mainModule, delegate*<IshtarRawThread*, void> frame)
    {
        var thread = IshtarGC.AllocateImmortal<IshtarRawThread>(mainModule);
        LibUV.uv_thread_create(out var threadId, executeRaw, (IntPtr)thread);
        *thread = new IshtarRawThread(threadId, frame, mainModule);
        return thread;
    }

    public IshtarThread* CreateThread(CallFrame* frame)
    {
        var thread = IshtarGC.AllocateImmortal<IshtarThread>(frame);
        var threadContext = IshtarGC.AllocateImmortal<IshtarThreadContext>(thread);
        LibUV.uv_thread_create(out var threadId, execute, (IntPtr)thread);
        LibUV.uv_sem_init(out var locker, 0);
        *threadContext = new IshtarThreadContext(threadId, locker);

        *thread = new IshtarThread(threadId, frame, threadContext);
        return thread;
    }

    public TaskScheduler* CreateScheduler(VirtualMachine vm) => TaskScheduler.Create(vm);
    public void FreeScheduler(TaskScheduler* scheduler) => TaskScheduler.Free(scheduler);

    public void Join(IshtarThread* thread)
        => LibUV.uv_thread_join(thread->threadId);

    private static void execute(nint arg)
    {
        var threadData = (IshtarThread*)arg;

        LibUV.uv_sem_wait(ref threadData->ctx->Locker);

        var frame = threadData->callFrame;
        var vm = threadData->callFrame->vm;

        var stackbase = new GC_stack_base();
        vm.GC.get_stack_base(&stackbase);
        vm.GC.register_thread(&stackbase);

        vm.exec_method(frame);

        vm.GC.unregister_thread();
    }

    private static void executeRaw(nint arg)
    {
        var thread = (IshtarRawThread*)arg;
        var stackbase = new GC_stack_base();

        BoehmGCLayout.Native.GC_get_stack_base(&stackbase);
        BoehmGCLayout.Native.GC_register_my_thread(&stackbase);
        thread->callFrame(thread);
        BoehmGCLayout.Native.GC_unregister_my_thread();
    }
}
