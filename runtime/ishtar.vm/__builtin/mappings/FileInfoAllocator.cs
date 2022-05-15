namespace ishtar;

using vein.runtime;

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
