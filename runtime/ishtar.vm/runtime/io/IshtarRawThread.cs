namespace ishtar.io;

using runtime;
using vm.libuv;

public readonly unsafe struct IshtarRawThread(
    LibUV.uv_thread_t threadId,
    delegate*<nint, void> frame,
    RuntimeIshtarModule* mainModule)
{
    public readonly RuntimeIshtarModule* MainModule = mainModule;
    public readonly LibUV.uv_thread_t threadId = threadId;
    public readonly delegate*<nint, void> callFrame = frame;
}