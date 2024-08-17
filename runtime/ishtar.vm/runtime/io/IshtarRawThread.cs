namespace ishtar.io;

using collections;
using runtime;
using libuv;

[CTypeExport("ishtar_thread_raw_t")]
public readonly unsafe struct IshtarRawThread(
    LibUV.uv_thread_t threadId,
    delegate*<IshtarRawThread*, void> frame,
    VirtualMachine* _vm,
    void* _data,
    InternedString* name) : IEq<IshtarRawThread>
{
    public readonly VirtualMachine* vm = _vm;
    public readonly LibUV.uv_thread_t threadId = threadId;
    public readonly delegate*<IshtarRawThread*, void> callFrame = frame;
    public readonly InternedString* Name = name;
    public readonly void* data = _data;
    public static bool Eq(IshtarRawThread* p1, IshtarRawThread* p2) => p1->threadId.handle == p2->threadId.handle;
}
