namespace vein.runtime;

using fs;

public class AssemblyBundle
{
    public FileInfo MainModulePath { get; set; }
    public List<byte> MainModuleBytes { get; set; }

    public List<IshtarAssembly> Assemblies { get; private set; }

        
    public static bool IsBundle(out AssemblyBundle bundle)
    {
        var current = Process.GetCurrentProcess()?.MainModule?.FileName;
        bundle = null;
        if (string.IsNullOrEmpty(current))
        {
            //VM.FastFail(WNE.STATE_CORRUPT, "Current executable has corrupted. [process file not found]", IshtarFrames.EntryPoint);
            return false;
        }

        var bytes = File.ReadAllBytes(current).ToList();
        var magicBytes = bytes.TakeLast(2).ToArray();

        if (BitConverter.ToInt16(magicBytes, 0) != 0x7ABC)
            return false;
        bundle = new AssemblyBundle
        {
            MainModuleBytes = bytes,
            MainModulePath = new FileInfo(current)
        }.UnpackAssemblies();

        return true;
    }


    private AssemblyBundle UnpackAssemblies()
    {
        Assemblies = new List<IshtarAssembly>();


        var offset_bytes = MainModuleBytes.SkipLast(sizeof(short)).TakeLast(sizeof(int)).ToArray();
        var offset = BitConverter.ToInt32(offset_bytes);

        var input = MainModuleBytes.SkipLast(sizeof(short) + sizeof(int)).Skip(offset).ToArray();
        using var mem = new MemoryStream(input); // todo multiple modules
        Assemblies.Add(IshtarAssembly.LoadFromMemory(mem));

        return this;
    }
}
