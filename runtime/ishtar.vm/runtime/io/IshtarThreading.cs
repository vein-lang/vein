namespace ishtar.io;

using collections;
using runtime;
using runtime.gc;
using static libuv.LibUV;


[CTypeExport("ishtar_threading_t")]
public readonly unsafe struct IshtarThreading(VirtualMachine* vm)
{
    public readonly NativeList<IshtarThread>* threads = IshtarGC.AllocateList<IshtarThread>(vm);

    public IshtarRawThread* CreateRawThread(delegate*<IshtarRawThread*, void> frame, void* data, string name)
    {
        lock (locker)
        {
            var thread = IshtarGC.AllocateImmortal<IshtarRawThread>(vm);
            //uv_thread_create(out var threadId, executeRaw, (IntPtr)thread);
            
            var t = new Thread((x) =>
            {
                executeRaw((nint)x!);
            });

            *thread = new IshtarRawThread(new uv_thread_t {handle = t.ManagedThreadId}, frame, vm, data, StringStorage.Intern(name, vm));

            t.Start((IntPtr)thread);

            return thread;
        }
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

    private static readonly object locker = new();
    private static void executeRaw(nint arg)
    {
        var thread = (IshtarRawThread*)arg;
#if DEBUG
        Thread.CurrentThread.Name = StringStorage.GetStringUnsafe(thread->Name);
#endif
        var stackbase = default(GC_stack_base);
        if (!BoehmGCLayout.Native.GC_thread_is_registered())
        {
            BoehmGCLayout.Native.GC_get_stack_base2(out var gcInfo);
            BoehmGCLayout.Native.GC_register_my_thread2(gcInfo);
        }
        thread->callFrame(thread);
        BoehmGCLayout.Native.GC_unregister_my_thread();
    }

    public static void SetName(string s)
    {
#if DEBUG
        if (string.IsNullOrEmpty(System.Threading.Thread.CurrentThread.Name))
            System.Threading.Thread.CurrentThread.Name = s;
#endif
    }
}
