namespace ishtar;

using vein.runtime;
using static vein.runtime.MethodFlags;
using static vein.runtime.VeinTypeCode;

public static unsafe class B_File
{
    [IshtarExport(1, "i_call_fs_File_info")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* GetFileInfo(CallFrame current, IshtarObject** args)
    {
        var raw = args[0];


        FFI.StaticValidate(current, &raw);
        FFI.StaticTypeOf(current, &raw, TYPE_STRING);

        FFI.StaticValidateField(current, &raw, "!!value");

        var path = IshtarMarshal.ToDotnetString(raw, current);


        var fi = new FileInfo(path);


        return Marshals.GetFor<FileInfo>(current).Marshal(fi, current);
    }


    internal static IshtarMetaClass ThisClass => IshtarMetaClass.Define("vein/lang/fs", "File");


    public static void InitTable(Dictionary<string, RuntimeIshtarMethod> table)
    {
        new RuntimeIshtarMethod("i_call_fs_File_info", Public | Static | Extern,
                new VeinArgumentRef("path", VeinCore.StringClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&GetFileInfo)
            .AddInto(table, x => x.Name);
    }
}


public unsafe struct Pointer1D<T>
{
    public Pointer1D(void* p) => _ref = p;
    public void* _ref;
}

public unsafe struct Pointer2D<T>
{
    public Pointer2D(void** p) => _ref = p;
    public void** _ref;
}

public interface ITransitAllocator {}

public static class Marshals
{
    private static Dictionary<Type, ITransitAllocator> _list = new();

    public static TransitAllocator<T>? GetFor<T>(CallFrame frame) where T : class
    {
        var key = typeof(T);

        if (_list.ContainsKey(key))
            return _list[typeof(T)] as TransitAllocator<T>;
        
        frame.ThrowException(KnowTypes.TypeNotFoundFault(frame), "failed fetch transit allocator");

        return null;
    }


    public static void Setup()
    {
        _list.Add(typeof(FileInfo), new FileInfoAllocator());
    }
}

public unsafe class FileInfoAllocator : TransitAllocator<FileInfo>
{
    public override unsafe IshtarObject* Marshal(FileInfo t, CallFrame frame)
    {
        //IshtarSync.EnterCriticalSection(ref frame.Owner.Interlocker.INIT_TYPE_BARRIER);
        
        var @this = RuntimeType(frame);
        var obj = IshtarGC.AllocObject(@this);


        obj->vtable[@this.Field["_exist"].vtable_offset] = IshtarMarshal.ToIshtarObject(t.Exists);
        obj->vtable[@this.Field["_name"].vtable_offset] = IshtarMarshal.ToIshtarObject(t.Name);
        obj->vtable[@this.Field["_full_name"].vtable_offset] = IshtarMarshal.ToIshtarObject(t.FullName);
        obj->vtable[@this.Field["_length"].vtable_offset] = IshtarMarshal.ToIshtarObject(t.Length);


        return obj;

        //IshtarSync.EnterCriticalSection(ref @class.Owner.Interlocker.INIT_TYPE_BARRIER);
    }

    public override VeinClass Type => IshtarMetaClass.Define("vein/lang/fs", "File");
}

public unsafe abstract class TransitAllocator<T> : ITransitAllocator where T : class 
{
    public abstract IshtarObject* Marshal(T t, CallFrame frame);

    public abstract VeinClass Type { get; }

    public RuntimeIshtarClass RuntimeType(CallFrame frame)
        => KnowTypes.FromCache(Type.FullName, frame);
}
