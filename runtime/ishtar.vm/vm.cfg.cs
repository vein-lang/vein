namespace ishtar;

using ishtar.runtime.io;
using ishtar.runtime.io.ini;
using runtime.gc;

public unsafe partial struct VirtualMachine
{
    public static IniRoot* readBootCfg()
    {
        using var tag = Profiler.Begin("vm:readBootCfg");
        var path = "";

        if (IshtarFile.exist("./obj/boot.ini"))
            path = "./obj/boot.ini";
        else if (IshtarFile.exist("./boot.ini"))
            path = "./boot.ini";
        else
            return null;

        var source = IshtarFile.readAllFile(path);

        var parser = new IniParser(source, IshtarGC.CreateAllocatorWithParent(null));
        

        var result = parser.Parse();

        result->_cache_sliced = source.Ptr;

        return result;
    }
}
