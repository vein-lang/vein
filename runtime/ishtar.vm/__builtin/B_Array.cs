namespace ishtar;

using runtime;

/// <summary>
/// Native backing for the std <c>Array&lt;T&gt;</c> class (<c>std/Array.any.vein</c>). The Vein class only
/// declares the native slots (<c>!!value</c>/<c>!!block</c>/<c>!!size</c>/<c>!!rank</c>); the actual storage,
/// allocation and indexing live here.
///
/// Elements are kept as boxed/reference <see cref="IshtarObject"/> pointers in a GC-scanned buffer
/// (so stored values stay alive). On read the FFI return path (<c>exec_method_internal_native</c>)
/// unboxes value types back to their primitive stackval based on the element's runtime class, so a
/// round-trip <c>Set(i, 42); Get(i)</c> yields a plain <c>i32</c> again — no per-instantiation element
/// class is required, which keeps this independent of the reified-generics layout work.
/// </summary>
public static unsafe class B_Array
{
    private static ulong OffsetValue(IshtarObject* self) => self->clazz->Field["!!value"]->vtable_offset;
    private static ulong OffsetBlock(IshtarObject* self) => self->clazz->Field["!!block"]->vtable_offset;
    private static ulong OffsetSize(IshtarObject* self) => self->clazz->Field["!!size"]->vtable_offset;
    private static ulong OffsetRank(IshtarObject* self) => self->clazz->Field["!!rank"]->vtable_offset;

    private static IshtarObject** Buffer(IshtarObject* self) => (IshtarObject**)self->vtable[OffsetValue(self)];
    private static ulong Length(IshtarObject* self) => (ulong)self->vtable[OffsetSize(self)];

    /// <summary>arr_alloc(this, size) — turn a freshly-constructed Array&lt;T&gt; into a backed buffer.</summary>
    private static IshtarObject* arr_alloc(CallFrame* frame, IshtarObject** args)
    {
        var self = args[0];
        var size = (ulong)IshtarMarshal.ToDotnetInt32(args[1], frame);

        ForeignFunctionInterface.StaticValidateField(frame, &self, "!!value");
        ForeignFunctionInterface.StaticValidateField(frame, &self, "!!size");

        // GC-scanned slots so the element references we store keep their targets alive.
        var buffer = frame->GetGC()->AllocVTable((uint)size);

        self->vtable[OffsetValue(self)] = buffer;
        self->vtable[OffsetBlock(self)] = (void*)(ulong)sizeof(void*);
        self->vtable[OffsetSize(self)]  = (void*)size;
        self->vtable[OffsetRank(self)]  = (void*)1UL;
        return null;
    }

    /// <summary>arr_set(this, index, value) — store an element.</summary>
    private static IshtarObject* arr_set(CallFrame* frame, IshtarObject** args)
    {
        var self = args[0];
        var index = (ulong)IshtarMarshal.ToDotnetInt32(args[1], frame);
        var value = args[2];

        if (index >= Length(self))
        {
            frame->vm->FastFail(WNE.OUT_OF_RANGE, $"Array.Set index {index} is out of range (length {Length(self)}).", frame);
            return null;
        }

        Buffer(self)[index] = value;
        return null;
    }

    /// <summary>arr_get(this, index) — load an element (value types are unboxed by the FFI return path).</summary>
    private static IshtarObject* arr_get(CallFrame* frame, IshtarObject** args)
    {
        var self = args[0];
        var index = (ulong)IshtarMarshal.ToDotnetInt32(args[1], frame);

        if (index >= Length(self))
        {
            frame->vm->FastFail(WNE.OUT_OF_RANGE, $"Array.Get index {index} is out of range (length {Length(self)}).", frame);
            return null;
        }

        var el = Buffer(self)[index];
        if (el is null)
        {
            frame->vm->FastFail(WNE.ACCESS_VIOLATION, $"Array.Get index {index} read an uninitialized element.", frame);
            return null;
        }
        return el;
    }

    public static IshtarObject* __indexer_getter(CallFrame* current, IshtarObject** args)
        => arr_get(current, args);

    public static void InitTable(ForeignFunctionInterface ffi)
    {
        ffi.Add("__indexer_getter([std]::std::Object,[std]::std::Int32) -> [std]::std::Object", ffi.AsNative(&__indexer_getter));
        ffi.Add("arr_alloc([std]::std::Object,[std]::std::Int32) -> [std]::std::Void", ffi.AsNative(&arr_alloc));
        ffi.Add("arr_set([std]::std::Object,[std]::std::Int32,[std]::std::Object) -> [std]::std::Void", ffi.AsNative(&arr_set));
        ffi.Add("arr_get([std]::std::Object,[std]::std::Int32) -> [std]::std::Object", ffi.AsNative(&arr_get));
    }
}
