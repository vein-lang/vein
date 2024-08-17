namespace ishtar.runtime.io;

using collections;
using ishtar.io;
using ishtar.runtime.gc;
using libuv;
using static gc.IshtarGC;
using static IshtarMath;
using static libuv.LibUV;
using static WNE;

public readonly unsafe struct IshtarThreadPool(
    VirtualMachine* vm,
    NativeSortedSet<IshtarTask>* tasks,
    NativeList<IshtarRawThread>* threads,
    uv_mutex_t* mutex,
    uv_cond_t* cond,
    int threadCount,
    IshtarThreadPool* self)
{
    private readonly VirtualMachine* _vm = vm;
    private readonly NativeSortedSet<IshtarTask>* _tasks = tasks;
    private readonly NativeList<IshtarRawThread>* _threads = threads;
    private readonly uv_mutex_t* _mutex = mutex;
    private readonly uv_cond_t* _cond = cond;
    private readonly int _threadCount = threadCount;
    private readonly IshtarThreadPool* _self = self;

    public static IshtarThreadPool* Create(VirtualMachine* vm)
    {
        using var tag = Profiler.Begin("vm:threadpool:create");
        uv_cpu_info_t cpuInfo;
        uv_cpu_info(&cpuInfo, out var coresCount);

        var overrideSize = (int)vm->Config.ThreadPoolSize;
        var thread_count = max(min(coresCount * 2, 16), 4);
        if (overrideSize != -1)
            thread_count = max(min(overrideSize, 128), 4);

        var pool = AllocateImmortal<IshtarThreadPool>(vm);
        var tasks = AllocateSortedSet<IshtarTask>(pool);
        var threads = AllocateList<IshtarRawThread>(pool, thread_count);
        var mutex = AllocateImmortal<uv_mutex_t>(pool);
        var cond = AllocateImmortal<uv_cond_t>(pool);

        vm->Assert(uv_mutex_init(mutex), THREAD_POOL_CORRUPTED, "uv_mutex_init", vm->Frames->ThreadScheduler);
        vm->Assert(uv_cond_init(cond), THREAD_POOL_CORRUPTED, "uv_cond_init", vm->Frames->ThreadScheduler);

        *pool = new IshtarThreadPool(vm, tasks, threads, mutex, cond, thread_count, pool);

        if (!vm->Config.DeferThreadPool)
            pool->populate();

        return pool;
    }

    private static void thread_runner(IshtarRawThread* arg)
    {
        var pool = (IshtarThreadPool*)arg->data;
        var vm = pool->_vm;
        var queue = pool->_tasks;
        
        while (true)
        {
            uv_mutex_lock(pool->_mutex);

            while (queue->Count == 0 && !vm->HasStopped())
                uv_cond_timedwait(pool->_cond, pool->_mutex, 10000);

            if (vm->HasStopped() && queue->Count == 0)
            {
                uv_mutex_unlock(pool->_mutex);
                break;
            }

            var task = queue->min();
            queue->remove(task);

            uv_mutex_unlock(pool->_mutex);

            vm->exec_method(task->Frame);
            uv_sem_post(ref task->Data->semaphore);

            destroyTask(task);
        }
    }

    private void populate()
    {
        for (int i = 0; i < _threadCount; i++)
        {
            var thread = vm->threading.CreateRawThread(&thread_runner, _self, $"pool:thread:{i}");
            _threads->Add(thread);
        }
    }
    public void createTask(CallFrame* frame, TaskPriority priority = TaskPriority.NORMAL)
    {
        if (threads->Count == 0) populate();

        var task = AllocateImmortal<IshtarTask>(frame);

        *task = new IshtarTask(frame, 0, priority);

        uv_sem_init(out task->Data->semaphore, 0);

        uv_mutex_lock(_mutex);
        _tasks->add(task);
        uv_cond_signal(_cond);
        uv_mutex_unlock(_mutex);
    }

    private static void destroyTask(IshtarTask* task)
    {
        uv_sem_wait(ref task->Data->semaphore);
        uv_sem_destroy(ref task->Data->semaphore);
        task->Dispose();
        FreeImmortal(task);
    }

    public void Stop()
    {
        uv_mutex_lock(_mutex);
        uv_cond_signal(_cond);
        uv_mutex_unlock(_mutex);

        for (int i = 0; i < _threadCount; i++)
        {
            var t = _threads->Get(i);
            uv_thread_join(t->threadId);
        }

        uv_mutex_destroy(_mutex);
        uv_cond_destroy(_cond);
    }
}


public struct ishtar_cond_v
{

}
