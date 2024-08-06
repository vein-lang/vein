namespace ishtar.io;

using collections;
using runtime.gc;
using libuv;

[CTypeExport("ishtar_task_t")]
public readonly unsafe struct IshtarTask(CallFrame* frame, ulong index) : IEq<IshtarTask>, IDisposable
{
    public readonly ulong Index = index;
    public readonly TaskData* Data = IshtarGC.AllocateImmortal<TaskData>(frame);
    public readonly CallFrame* Frame = frame;

    public static bool Eq(IshtarTask* p1, IshtarTask* p2) => false;

    [CTypeExport("ishtar_task_data_t")]
    public struct TaskData
    {
        public LibUV.uv_sem_t semaphore;
    }

    public void Dispose() => IshtarGC.FreeImmortal(Data);
}
