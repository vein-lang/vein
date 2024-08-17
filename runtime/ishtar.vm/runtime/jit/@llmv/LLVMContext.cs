namespace ishtar.llmv;

using LLVMSharp.Interop;
using vein.extensions;
using vein.runtime;

public unsafe struct LLVMContext
{
    private LLVMOpaqueContext* _ctx;
    private LLVMModuleRef _ffiModule;
    private LLVMExecutionEngineRef _executionEngine;
    private bool is_inited;
    public LLVMContext(VirtualMachine* vm)
    {
        using var tag = Profiler.Begin("vm:llvm:init");

        if (vm->Config.Jit.EnableTargetMC)
            LLVM.LinkInMCJIT();
        if (vm->Config.Jit.EnableAsmPrinter)
            LLVM.InitializeNativeAsmPrinter();
        if (vm->Config.Jit.EnableAsmParser)
            LLVM.InitializeNativeAsmParser();
        if (vm->Config.Jit.IsAutoTarget)
            LLVM.InitializeNativeTarget();

        if (!vm->Config.Jit.DeferContext) deferInit();
    }

    private void deferInit()
    {
        if (is_inited) return;
        _ctx = LLVM.ContextCreate();

        if (_ffiModule == default)
            _ffiModule = LLVMModuleRef.CreateWithName("_ffi");

        _executionEngine = _ffiModule.CreateExecutionEngine();
        is_inited = true;
    }

    public LLVMExecutionEngineRef GetExecutionEngine()
    {
        if (!is_inited) deferInit();
        return _executionEngine;
    }

    public LLVMModuleRef GetExecutionModule()
    {
        if (!is_inited) deferInit();
        return _ffiModule;
    }

    [Conditional("DEBUG")]
    public void PrintAsm(RuntimeIshtarMethod* method)
        => IshtarJIT.DumpExecutableProcedure(
            ReadUntil((byte*)method->PIInfo.compiled_func_ref).ToArray());

    private static Span<byte> ReadUntil(byte* function)
    {
        var size = 0;
        var offset = function;
        while (true)
        {
            if (*offset == 0xC3)
            {
                size++;
                break;
            }
            size++;
            offset++;
        }
        return new Span<byte>(function, size);
    }

    public PInvokeInfo CompileFFIWithSEH(string methodName, string moduleName, string fnName, List<VeinTypeCode> args, VeinTypeCode returnType)
    {
        if (!is_inited) deferInit();

        var retType = GetLLVMType(returnType);
        var args_converted = args.Select(GetLLVMType).ToList();

        var externalFuncType = LLVMTypeRef.CreateFunction(retType, args_converted.ToArray(), false);
        var externalFunc = _ffiModule.AddFunction($"{moduleName}_{fnName}", externalFuncType);

        var funcType = LLVMTypeRef.CreateFunction(retType, args_converted.ToArray(), false);
        var function = _ffiModule.AddFunction(fnName, funcType);

        var builder = LLVMBuilderRef.Create(_ffiModule.Context);
        var entry = function.AppendBasicBlock("entry");
        var tryBlock = function.AppendBasicBlock("try");
        var landingPadBlock = function.AppendBasicBlock("landingpad");
        var contBlock = function.AppendBasicBlock("cont");
        var returnBlock = function.AppendBasicBlock("return");

        builder.PositionAtEnd(entry);
        builder.BuildBr(tryBlock);

        builder.PositionAtEnd(tryBlock);

        var args_direct = new List<LLVMValueRef>();
        for (int i = 0; i < args_converted.Count; i++)
            args_direct.Add(function.GetParam((uint)i));

        var invoke = builder.BuildInvoke2(externalFuncType, externalFunc, args_direct.ToArray(), contBlock, landingPadBlock, "invoke");

        builder.PositionAtEnd(contBlock);
        if (returnType != VeinTypeCode.TYPE_VOID)
        {
            builder.BuildBr(returnBlock);
        }

        builder.PositionAtEnd(landingPadBlock);

        var personalityFnType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Int32, Array.Empty<LLVMTypeRef>(), false);
        var personalityFn = _ffiModule.AddFunction("__gxx_personality_v0", personalityFnType);

        var landingPad = builder.BuildLandingPad(
        LLVMTypeRef.CreateStruct([LLVMTypeRef.Int32, LLVMTypeRef.Int32], false),
        personalityFn,
        1,
        "landingpad");
        landingPad.AddClause(LLVMValueRef.CreateConstNull(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)));

        builder.BuildBr(returnBlock);

        builder.PositionAtEnd(returnBlock);
        if (returnType == VeinTypeCode.TYPE_VOID)
        {
            builder.BuildRetVoid();
        }
        else
        {
            var retPhi = builder.BuildPhi(retType, "retPhi");
            retPhi.AddIncoming([invoke], [contBlock], 1);
            retPhi.AddIncoming([LLVMValueRef.CreateConstNull(retType)], [landingPadBlock], 1);
            builder.BuildRet(retPhi);
        }

        var nativeModuleHandle = NativeLibrary.Load(moduleName);

        return new PInvokeInfo()
        {
            module_handle = nativeModuleHandle,
            symbol_handle = NativeLibrary.GetExport(nativeModuleHandle, fnName),
            extern_function_declaration = externalFunc,
            jitted_wrapper = function
        };
    }

    public void CompileRawFFI(RuntimeIshtarMethod* method, string moduleName, string fnName)
    {
        if (!is_inited) deferInit();

        if (_ffiModule == default)
            _ffiModule = LLVMModuleRef.CreateWithName("_ffi");

        var retType = GetLLVMType(method->ReturnType);
        var args = new List<LLVMTypeRef>();
        method->Arguments->ForEach(x =>
        {
            args.Add(GetLLVMType(x->Type));
        });
        
        var externalFuncType = LLVMTypeRef.CreateFunction(retType, args.ToArray(), false);
        var externalFunc = _ffiModule.AddFunction($"{moduleName}_{fnName}", externalFuncType);


        var funcType = LLVMTypeRef.CreateFunction(retType, args.ToArray(), false);
        var function = _ffiModule.AddFunction(fnName, funcType);

        var builder = LLVMBuilderRef.Create(_ffiModule.Context);
        var entry = function.AppendBasicBlock("body");
        builder.PositionAtEnd(entry);


        var args_direct = new List<LLVMValueRef>();

        foreach (var i in ..method->ArgLength)
            args_direct.Add(function.GetParam((uint)i));

        var returnValue = builder.BuildCall2(externalFuncType, externalFunc, args_direct.ToArray(), string.Empty);
        if (method->ReturnType->TypeCode is VeinTypeCode.TYPE_VOID)
            builder.BuildRetVoid();
        else 
            builder.BuildRet(returnValue);
        
        var nativeModuleHandle = NativeLibrary.Load(moduleName);

        method->PIInfo = new PInvokeInfo
        {
            module_handle = nativeModuleHandle,
            symbol_handle = NativeLibrary.GetExport(nativeModuleHandle, fnName),
            extern_function_declaration = externalFunc,
            jitted_wrapper = function
        };
    }

    public void CompileFFI(RuntimeIshtarMethod* method, string moduleName, string fnName)
    {
        if (!is_inited) deferInit();

        var maxSize = Math.Max(Math.Max(sizeof(decimal), sizeof(Half)), IntPtr.Size);
        var stackUnionType = LLVMTypeRef.CreateArray(LLVMTypeRef.Int8, (uint)maxSize);
        var stackValType = LLVMTypeRef.CreateStruct(new[] { stackUnionType, LLVMTypeRef.Int32 }, false);

        var retType = GetLLVMType(method->ReturnType);
        var argTypes = new List<LLVMTypeRef>();
        method->Arguments->ForEach(x => {
            argTypes.Add(GetLLVMType(x->Type));
        });
        
        var externalFuncType = LLVMTypeRef.CreateFunction(retType, argTypes.ToArray(), false);
        var externalFunc = _ffiModule.AddFunction($"{moduleName}_{fnName}", externalFuncType);
        
        var stackvalPtr = LLVMTypeRef.CreatePointer(stackValType, 0);
        var wrappedFuncType = LLVMTypeRef.CreateFunction(stackValType, new[] { stackvalPtr, LLVMTypeRef.Int32 }, false);
        var function = _ffiModule.AddFunction(fnName, wrappedFuncType);

        var builder = LLVMBuilderRef.Create(_ffiModule.Context);
        var entry = function.AppendBasicBlock("entry");
        builder.PositionAtEnd(entry);


        var argArrayPtr = function.GetParam(0);

        var argsDirect = new List<LLVMValueRef>();
        for (int i = 0; i < method->Arguments->Count; i++)
        {
            var index = builder.BuildGEP2(stackvalPtr, argArrayPtr, new[] { LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)i, false) }, $"argPtr{i}");
            var stackval_ref = builder.BuildLoad2(LLVMTypeRef.Int32, index, $"stackValPtr{i}");
            var dataPtr = builder.BuildStructGEP2(stackvalPtr, stackval_ref, 0, $"dataPtr{i}");
            var castedDataPtr = builder.BuildBitCast(dataPtr, LLVMTypeRef.CreatePointer(argTypes[i], 0), $"castedDataPtr{i}");
            var argValue = builder.BuildLoad2(stackvalPtr, castedDataPtr, $"argValue{i}");
            argsDirect.Add(argValue);
        }

        LLVMValueRef returnValue = null;
        if (method->ReturnType->TypeCode != VeinTypeCode.TYPE_VOID)
            returnValue = builder.BuildCall2(externalFuncType, externalFunc, argsDirect.ToArray(), string.Empty);
        else
            builder.BuildCall2(externalFuncType, externalFunc, argsDirect.ToArray(), string.Empty);

        var retStackVal = builder.BuildAlloca(stackValType, "retStackVal");
        if (method->ReturnType->TypeCode != VeinTypeCode.TYPE_VOID)
        {
            var retTypePtr_Ref = LLVMTypeRef.CreatePointer(retType, 0);
            var retDataPtr = builder.BuildStructGEP2(stackValType, retStackVal, 0, "retDataPtr");
            var castedRetDataPtr = builder.BuildBitCast(retDataPtr, retTypePtr_Ref, "castedRetDataPtr");
            builder.BuildStore(returnValue, castedRetDataPtr);

            var retTypePtr = builder.BuildStructGEP2(stackValType, retStackVal, 1, "retTypePtr");
            builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)method->ReturnType->TypeCode, false), retTypePtr);
        }
        else
        {
            var retTypePtr = builder.BuildStructGEP2(stackValType, retStackVal, 1, "retTypePtr");
            builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)VeinTypeCode.TYPE_VOID, false), retTypePtr);
        }

        var retVal = builder.BuildLoad2(stackValType, retStackVal, "retVal");
        builder.BuildRet(retVal);

        var nativeModuleHandle = NativeLibrary.Load(moduleName);

        method->PIInfo = new PInvokeInfo
        {
            module_handle = nativeModuleHandle,
            symbol_handle = NativeLibrary.GetExport(nativeModuleHandle, fnName),
            extern_function_declaration = externalFunc,
            jitted_wrapper = function
        };
    }

    

    public class DeOptimizationDetected : Exception;

    private static LLVMTypeRef GetLLVMType(VeinTypeCode typeCode) =>
        typeCode switch
        {
            VeinTypeCode.TYPE_BOOLEAN => LLVMTypeRef.Int1,
            VeinTypeCode.TYPE_I1 or VeinTypeCode.TYPE_U1 => LLVMTypeRef.Int8,
            VeinTypeCode.TYPE_I2 or VeinTypeCode.TYPE_U2 => LLVMTypeRef.Int16,
            VeinTypeCode.TYPE_I4 or VeinTypeCode.TYPE_U4 => LLVMTypeRef.Int32,
            VeinTypeCode.TYPE_I8 or VeinTypeCode.TYPE_U8 => LLVMTypeRef.Int64,
            VeinTypeCode.TYPE_VOID => LLVMTypeRef.Void,
            VeinTypeCode.TYPE_R2 => LLVMTypeRef.Half,
            VeinTypeCode.TYPE_R4 => LLVMTypeRef.Float,
            VeinTypeCode.TYPE_R8 => LLVMTypeRef.Double,
            VeinTypeCode.TYPE_R16 => LLVMTypeRef.PPCFP128,
            VeinTypeCode.TYPE_RAW => LLVMTypeRef.CreatePointer(LLVMTypeRef.Int64, 0),
            VeinTypeCode.TYPE_STRING => LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0),
            VeinTypeCode.TYPE_CHAR => LLVMTypeRef.Int32,
            VeinTypeCode.TYPE_FUNCTION => LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0),
            _ => throw new DeOptimizationDetected()
        };

    private static LLVMTypeRef GetLLVMType(RuntimeIshtarClass* clazz)
    {
        var type = clazz->TypeCode;
        return GetLLVMType(type);
    }

}
