namespace ishtar.io;

using collections;
using runtime.gc;
using libuv;
using LLVMSharp;

[CTypeExport("ishtar_task_t")]
public readonly unsafe struct IshtarTask(CallFrame* frame, ulong index, TaskPriority priority) : IEq<IshtarTask>, IDisposable, INativeComparer<IshtarTask>
{
    public readonly ulong Index = index;
    public readonly TaskData* Data = IshtarGC.AllocateImmortal<TaskData>(frame);
    public readonly CallFrame* Frame = frame;
    public readonly TaskPriority Priority = priority;

    public static bool Eq(IshtarTask* p1, IshtarTask* p2) => ((nint)p1) == ((nint)p2);

    [CTypeExport("ishtar_task_data_t")]
    public struct TaskData
    {
        public LibUV.uv_sem_t semaphore;
    }

    public void Dispose() => IshtarGC.FreeImmortal(Data);
    public static int Compare(IshtarTask* p1, IshtarTask* p2)
    {
        if (p1->Priority < p2->Priority)
            return -1;
        return p1->Priority > p2->Priority ? 1 : 0;
    }
}

public enum TaskPriority
{
    ULTRA_LOWER = -2,
    LOWER = -1,
    NORMAL = 0,
    HIGH = 1,
    EXTREME = 2
}
