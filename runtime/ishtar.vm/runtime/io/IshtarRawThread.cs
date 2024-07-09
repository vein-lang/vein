namespace ishtar.io;

using runtime;
using libuv;

public readonly unsafe struct IshtarRawThread(
    LibUV.uv_thread_t threadId,
    delegate*<IshtarRawThread*, void> frame,
    RuntimeIshtarModule* mainModule)
{
    public readonly RuntimeIshtarModule* MainModule = mainModule;
    public readonly LibUV.uv_thread_t threadId = threadId;
    public readonly delegate*<IshtarRawThread*, void> callFrame = frame;
}
