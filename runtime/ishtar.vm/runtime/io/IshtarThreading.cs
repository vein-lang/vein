namespace ishtar.io;

using runtime;
using runtime.gc;
using vm.libuv;

public unsafe struct IshtarThreading
{
    public IshtarRawThread* CreateRawThread(RuntimeIshtarModule* mainModule, delegate*<nint, void> frame)
    {
        var thread = IshtarGC.AllocateImmortal<IshtarRawThread>();
        LibUV.uv_thread_create(out var threadId, executeRaw, (IntPtr)thread);
        *thread = new IshtarRawThread(threadId, frame, mainModule);
        return thread;
    }

    public IshtarThread* CreateThread(CallFrame* frame)
    {
        var thread = IshtarGC.AllocateImmortal<IshtarThread>();
        LibUV.uv_thread_create(out var threadId, execute, (IntPtr)thread);
        *thread = new IshtarThread(threadId, frame);
        return thread;
    }

    public TaskScheduler* CreateScheduler() => TaskScheduler.Create();
    public void FreeScheduler(TaskScheduler* scheduler) => TaskScheduler.Free(scheduler);

    public void Join(IshtarThread* thread)
        => LibUV.uv_thread_join(thread->threadId);

    private static void execute(nint arg)
    {
        var threadData = (IshtarThread*)arg;

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
        var stackbase = new GC_stack_base();

        BoehmGCLayout.Native.GC_get_stack_base(&stackbase);
        BoehmGCLayout.Native.GC_register_my_thread(&stackbase);
        ((IshtarRawThread*)arg)->callFrame(arg);
        BoehmGCLayout.Native.GC_unregister_my_thread();
    }
}