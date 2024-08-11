namespace ishtar;

using ishtar.runtime.io;
using ishtar.runtime.io.ini;
using runtime.gc;

public unsafe partial class VirtualMachine
{
    public static IniRoot* readBootCfg(VirtualMachineRef* @ref)
    {
        var path = "";

        if (IshtarFile.exist("./obj/boot.ini"))
            path = "./obj/boot.ini";
        else if (IshtarFile.exist("./boot.ini"))
            path = "./boot.ini";
        else
            return null;

        var source = IshtarFile.readAllFile(path);

        var parser = new IniParser(source, IshtarGC.CreateAllocatorWithParent(@ref));

        return parser.Parse();
    }
}
