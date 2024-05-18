namespace ishtar;

using vein.runtime;

public unsafe class FileInfoAllocator : TransitAllocator<FileInfo>
{
    public override unsafe IshtarObject* Marshal(FileInfo t, CallFrame frame)
    {
        //IshtarSync.EnterCriticalSection(ref frame.Owner.Interlocker.INIT_TYPE_BARRIER);

        //var @this = RuntimeType(frame);
        //var gc = frame.GetGC();
        //var obj = gc.AllocObject(@this, frame);

        //obj->vtable[@this.Field["_exist"].vtable_offset] = gc.ToIshtarObject(t.Exists, frame);
        //obj->vtable[@this.Field["_name"].vtable_offset] = gc.ToIshtarObject(t.Name, frame);
        //obj->vtable[@this.Field["_full_name"].vtable_offset] = gc.ToIshtarObject(t.FullName, frame);
        //obj->vtable[@this.Field["_length"].vtable_offset] = gc.ToIshtarObject(t.Length, frame);

        throw null;
        //return obj;

        //IshtarSync.EnterCriticalSection(ref @class.Owner.Interlocker.INIT_TYPE_BARRIER);
    }

    public override VeinClass Type => IshtarMetaClass.Define("vein/lang/fs", "File");
}
