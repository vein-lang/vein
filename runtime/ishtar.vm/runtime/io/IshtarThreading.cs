namespace ishtar.io;

using collections;
using runtime;
using runtime.gc;
using static libuv.LibUV;


[CTypeExport("ishtar_threading_t")]
public unsafe struct IshtarThreading(VirtualMachine* vm)
{
    public NativeList<IshtarThread>* threads = IshtarGC.AllocateList<IshtarThread>(vm);

    public IshtarRawThread* CreateRawThread(RuntimeIshtarModule* mainModule, delegate*<IshtarRawThread*, void> frame, string name)
    {
        var thread = IshtarGC.AllocateImmortal<IshtarRawThread>(mainModule);
        uv_thread_create(out var threadId, executeRaw, (IntPtr)thread);
        *thread = new IshtarRawThread(threadId, frame, mainModule, StringStorage.Intern(name, mainModule));
        return thread;
    }

    public IshtarThread* CreateThread(CallFrame* frame)
    {
        var thread = IshtarGC.AllocateImmortal<IshtarThread>(frame);
        var threadContext = IshtarGC.AllocateImmortal<IshtarThreadContext>(thread);

        uv_thread_create(out var threadId, execute, (IntPtr)thread);
        uv_sem_init(out var locker, 0);
        *threadContext = new IshtarThreadContext(threadId, locker);
        *thread = new IshtarThread(threadId, frame, threadContext);

        frame->vm->println($"thread start {threadId}");

        threads->Add(thread);

        return thread;
    }

    public static void DestroyThread(IshtarThread* thread)
    {
        thread->callFrame->vm->println($"[thread] exit thread with status {thread->ctx->Status} {thread->threadId}");
        uv_sem_destroy(ref thread->ctx->Locker);
        IshtarGC.FreeImmortal(thread->ctx);
        IshtarGC.FreeImmortal(thread);
    }

    public TaskScheduler* CreateScheduler(VirtualMachine* vm) => TaskScheduler.Create(vm);
    public void FreeScheduler(TaskScheduler* scheduler) => TaskScheduler.Free(scheduler);

    private static void execute(nint arg)
    {
        var threadData = (IshtarThread*)arg;

        #if DEBUG
        Thread.CurrentThread.Name = $"Thread [{threadData->callFrame->method->Name}]";
        #endif  

        uv_sem_wait(ref threadData->ctx->Locker);

        var frame = threadData->callFrame;
        var vm = threadData->callFrame->vm;

        var stackbase = new GC_stack_base();
        vm->gc->get_stack_base(&stackbase);
        vm->gc->register_thread(&stackbase);

        vm->exec_method(frame);

        vm->gc->unregister_thread();

        threadData->complete();

        vm->threading.threads->Remove(threadData);
        DestroyThread(threadData);
    }

    private static void executeRaw(nint arg)
    {
        var thread = (IshtarRawThread*)arg;
        var stackbase = new GC_stack_base();

        #if DEBUG
        Thread.CurrentThread.Name = StringStorage.GetStringUnsafe(thread->Name);
        #endif

        BoehmGCLayout.Native.GC_get_stack_base(&stackbase);
        BoehmGCLayout.Native.GC_register_my_thread(&stackbase);
        thread->callFrame(thread);
        BoehmGCLayout.Native.GC_unregister_my_thread();
    }
}
