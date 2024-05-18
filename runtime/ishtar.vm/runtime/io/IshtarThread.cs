namespace ishtar.io;

public readonly unsafe struct IshtarThread(uint stackSize, uint* tid);


public sealed unsafe class IshtarThreading(VirtualMachine vm)
{
    public IshtarThread* CreateThread() => throw new NotImplementedException();
    public int GetExitCodeThread(IshtarThread* thread) => throw new NotImplementedException();
    public void ExitThread(IshtarThread* thread, int exitCode) => throw new NotImplementedException();
    public void Sleep(IshtarThread* thread, uint ms) => throw new NotImplementedException();
}
