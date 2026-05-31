namespace ishtar.io;

using collections;
using vein.runtime;

/// <summary>
/// Represents a suspended interpreter frame — saved state of an async method
/// that hit an AWAIT on a pending Job.
/// 
/// When the awaited Job completes, this frame is resumed on a worker thread:
/// the interpreter restores IP, eval stack, and locals, then continues execution.
/// 
/// Memory layout: [SuspendedFrame | eval stack snapshot... | locals snapshot...]
/// A single contiguous allocation; evalStack and locals point into the trailing data.
/// </summary>
[CTypeExport("ishtar_suspended_frame_t")]
public unsafe struct SuspendedFrame : IEq<SuspendedFrame>
{
    public RuntimeIshtarMethod* method;
    public uint* savedIP;
    public stackval* evalStack;
    public int evalStackDepth;
    public stackval* locals;
    public int localsCount;
    public CallFrame* parent;
    public IshtarAsyncJob* ownerJob;
    public IshtarAsyncJob* awaitedJob;
    public stackval* args;
    public int maxStack;
    public SuspendedFrame* next;
    public VirtualMachine* vm;

    public static bool Eq(SuspendedFrame* p1, SuspendedFrame* p2) => p1 == p2;
}
