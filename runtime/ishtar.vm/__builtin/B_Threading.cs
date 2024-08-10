namespace ishtar;

using io;
using libuv;
using static IshtarMath;

public static unsafe class B_Threading
{
    private static RuntimeIshtarClass* getThreadClass(CallFrame* current)
    {
        var threadTypeName = current->vm.Vault.GlobalFindTypeName("[std]::std::Thread");
        var result = current->vm.Vault.GlobalFindType(threadTypeName, true, true);

        return result;
    }

    private static IshtarObject* createThread(CallFrame* current, IshtarObject** args)
    {
        var fnBox = args[0];
        var clazz = fnBox->clazz;

        var scopePointer = fnBox->vtable[clazz->Field["_scope"]->vtable_offset];
        var fnPointer = fnBox->vtable[clazz->Field["_fn"]->vtable_offset];
        
        var isVolatile = scopePointer == null;
        var metadataRef = (rawval*)fnPointer;

        current->assert(metadataRef->type == VeinRawCode.ISHTAR_METHOD, WNE.TYPE_MISMATCH);

        var method = metadataRef->data.m;

        var childFrame = current->CreateChild(method);

        if (!isVolatile)
            throw null;
        
        var type = getThreadClass(current);
        var threadObj = current->vm.GC.AllocObject(type, current);

        threadObj->vtable[type->Field["_fn"]->vtable_offset] = fnBox;
        threadObj->vtable[type->Field["_threadRef"]->vtable_offset] = current->vm.threading.CreateThread(childFrame);

        return threadObj;
    }

    private static IshtarObject* sleep(CallFrame* current, IshtarObject** args)
    {
        var ms = ToIn32(args[0], current);
        var clampTime = min(max(abs(ms), 1), 600_000);
        LibUV.uv_sleep((uint)clampTime);
        return default;
    }
    private static IshtarThread* getThread(CallFrame* current, IshtarObject** args)
    {
        var threadObj = args[0];
        var type = getThreadClass(current);

        return (IshtarThread*)threadObj->vtable[type->Field["_threadRef"]->vtable_offset];
    }

    private static IshtarObject* join(CallFrame* current, IshtarObject** args)
    {
        var thread = getThread(current, args);
        thread->join();

        return default;
    }

    private static IshtarObject* start(CallFrame* current, IshtarObject** args)
    {
        var thread = getThread(current, args);

        thread->start();

        return default;
    }

    private static IshtarObject* not_implemented(CallFrame* current, IshtarObject** args)
    {
        return default;
    }


    public static void InitTable(ForeignFunctionInterface ffi)
    {
        ffi.Add("@_threading_create([std]::std::ThreadFunction) -> [std]::std::Thread", ffi.AsNative(&createThread));
        ffi.Add("@_threading_sleep([std]::std::Int32) -> [std]::std::Void", ffi.AsNative(&sleep));
        ffi.Add("@_threading_join([std]::std::Thread) -> [std]::std::Void", ffi.AsNative(&join));
        ffi.Add("@_threading_start([std]::std::Thread) -> [std]::std::Void", ffi.AsNative(&start));
        ffi.Add("@_threading_begin_affinity() -> [std]::std::Void", ffi.AsNative(&not_implemented));
        ffi.Add("@_threading_end_affinity() -> [std]::std::Void", ffi.AsNative(&not_implemented));
        ffi.Add("@_threading_begin_critical_region() -> [std]::std::Void", ffi.AsNative(&not_implemented));
        ffi.Add("@_threading_end_critical_region() -> [std]::std::Void", ffi.AsNative(&not_implemented));
        ffi.Add("@_threading_memory_barrier() -> [std]::std::Void", ffi.AsNative(&not_implemented));
        ffi.Add("@_threading_yield() -> [std]::std::Void", ffi.AsNative(&not_implemented));

        //
    }


    private static int ToIn32(IshtarObject* obj, CallFrame* frame) => IshtarMarshal.ToDotnetInt32(obj, frame);
}
