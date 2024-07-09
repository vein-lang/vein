namespace ishtar.llmv;

using LLVMSharp.Interop;
using System.Xml.Linq;

public struct vm_applet
{
    public LLVMModuleRef _module;
}


public unsafe struct comparer_applet
{

    [DllImport("libLLVM", EntryPoint = "LLVMParseIRInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int ParseIRInContext(
        LLVMOpaqueContext* ContextRef,
        LLVMOpaqueMemoryBuffer* MemBuf,
        out LLVMOpaqueModule* module,
        out sbyte* OutMessage);

    public static comparer_applet load(LLVMContextRef ctx, FileInfo info)
    {
        using var context = LLVMContextRef.Create();
        var module = context.CreateModuleWithName("MyModule");
        string irFilePath = info.FullName;
        string irText = File.ReadAllText(irFilePath);

        LLVMMemoryBufferRef buffer;
        using var bufferName = new MarshaledString("ir_code");
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(irText);
        unsafe
        {
            fixed (byte* ptr = bytes)
            {
                buffer = LLVM.CreateMemoryBufferWithMemoryRangeCopy((sbyte*)ptr, (UIntPtr)bytes.Length, bufferName);
            }
        }

        var hasSuccess = ParseIRInContext(ctx, buffer, out var parsedModule, out var err);
        var m = (LLVMModuleRef)parsedModule;
        m.Dump();
        var passManager = LLVMPassManagerRef.Create();
        passManager.AddBasicAliasAnalysisPass();
        passManager.AddInstructionCombiningPass();
        passManager.AddReassociatePass();
        passManager.AddGVNPass();
        passManager.AddCFGSimplificationPass();
        passManager.Run(module);
        var target = LLVMTargetRef.Targets.ToList().First(x => x.Name.Equals("x86-64"));
        var outputPath = "path/to/your/output.o";
        var targetMachine = target.CreateTargetMachine(target.Name, "generic", "",
            LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault,
            LLVMRelocMode.LLVMRelocDefault,
            LLVMCodeModel.LLVMCodeModelDefault);
        using var targetPath = new MarshaledString(new FileInfo("./output.o").FullName);
        sbyte s = 0;
        sbyte* s2 = &s;
        var emitSuccess = LLVM.TargetMachineEmitToFile(targetMachine, m, targetPath, LLVMCodeGenFileType.LLVMObjectFile, &s2);
        throw null;
    }
}
