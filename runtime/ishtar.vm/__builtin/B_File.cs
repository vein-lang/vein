namespace ishtar;

using static vein.runtime.MethodFlags;
using static vein.runtime.VeinTypeCode;

public static unsafe class B_File
{
    [IshtarExport(1, "i_call_fs_File_info")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* GetFileInfo(CallFrame* current, IshtarObject** args)
    {
        throw null;
        //var raw = args[0];


        //ForeignFunctionInterface.StaticValidate(current, &raw);
        //ForeignFunctionInterface.StaticTypeOf(current, &raw, TYPE_STRING);

        //ForeignFunctionInterface.StaticValidateField(current, &raw, "!!value");

        //var path = IshtarMarshal.ToDotnetString(raw, current);


        //var fi = new FileInfo(path);


        //return Marshals.GetFor<FileInfo>(current).Marshal(fi, current);
    }


    internal static IshtarMetaClass ThisClass => IshtarMetaClass.Define("std/fs", "File");


    //public static void InitTable(Dictionary<string, RuntimeIshtarMethod> table) => new RuntimeIshtarMethod("i_call_fs_File_info", Public | Static | Extern,
    //            new VeinArgumentRef("path", VeinCore.StringClass))
    //        .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&GetFileInfo)
    //        .AddInto(table, x => x.Name);
}
