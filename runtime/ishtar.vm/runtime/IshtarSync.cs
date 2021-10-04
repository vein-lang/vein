namespace ishtar
{
    using System.Threading;

    public static class IshtarSync
    {
        // temporary using CLR mutex, in future need import system function for init and control mutex
        public static void EnterCriticalSection(ref object @ref) => (@ref as Mutex)?.WaitOne();
        // temporary using CLR mutex, in future need import system function for init and control mutex
        public static void LeaveCriticalSection(ref object @ref) => (@ref as Mutex)?.ReleaseMutex();
    }
}
