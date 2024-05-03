namespace ishtar;

using Iced.Intel;

public class AssemblerArgumentConverter
{
    public static void GenerateAssemblerCode(List<x64_AssemblerStep> argumentInfos,
        List<object> argumentValues, nint nativeFunctionPtr, Assembler asm)
    {
        int numRegistersUsed = argumentInfos.Count(argInfo => argInfo.Instruction != x64_AssemblerStep.InstructionTarget.push);

        int stackSpaceNeeded = numRegistersUsed * 8;


        asm.push(AssemblerRegisters.rbp);
        asm.mov(AssemblerRegisters.rbp, AssemblerRegisters.rsp);
        asm.sub(AssemblerRegisters.rsp, stackSpaceNeeded);

        int valueIndex = 0;

        foreach (var argInfo in argumentInfos)
        {
            if (argInfo.Instruction == x64_AssemblerStep.InstructionTarget.push)
                asm.push(AssemblerRegisters.__[argInfo.StackOffset]);
            else
            {
                if (argumentValues[valueIndex] is IntPtr ptr)
                {

                    asm.mov(new AssemblerRegister64(argInfo.Register), AssemblerRegisters.__[ptr]);
                    valueIndex++;
                }
                else
                {
                    var val = argumentValues[valueIndex++];
                    if (val is byte b)
                        asm.mov(new AssemblerRegister64(argInfo.Register), b);
                    if (val is short s)
                        asm.mov(new AssemblerRegister64(argInfo.Register), s);
                    if (val is int i)
                        asm.mov(new AssemblerRegister64(argInfo.Register), i);
                    if (val is long l)
                        asm.mov(new AssemblerRegister64(argInfo.Register), l);
                    //if (val is float f)
                    //    asm.mov(xmm0, 12f);
                    if (val is char c)
                        asm.mov(new AssemblerRegister64(argInfo.Register), c);
                    if (val is bool bb)
                        asm.mov(new AssemblerRegister64(argInfo.Register), (byte)(bb ? 1 : 0));
                    if (val is float or double)
                        throw new NotSupportedException();
                }

                asm.call(AssemblerRegisters.__[nativeFunctionPtr]);
                asm.add(AssemblerRegisters.rsp, stackSpaceNeeded);
                asm.pop(AssemblerRegisters.rbp);
                asm.ret();
            }
        }


    }

    public static List<x64_AssemblerStep> ConvertArguments(List<Type> argumentTypes, List<object> argumentValues)
    {
        Dictionary<Type, Register[]> availableRegisters = new()
        {
            { typeof(byte), [AssemblerRegisters.dl, AssemblerRegisters.cl, AssemblerRegisters.r8, AssemblerRegisters.r9, AssemblerRegisters.r12, AssemblerRegisters.r13] },
            { typeof(short), [AssemblerRegisters.dx, AssemblerRegisters.cx, AssemblerRegisters.r8, AssemblerRegisters.r9, AssemblerRegisters.r12, AssemblerRegisters.r13] },
            { typeof(int), [AssemblerRegisters.r9, AssemblerRegisters.r8, AssemblerRegisters.ecx, AssemblerRegisters.edx, AssemblerRegisters.r12, AssemblerRegisters.r13] },
            { typeof(long), [AssemblerRegisters.r8, AssemblerRegisters.rcx, AssemblerRegisters.rdx, AssemblerRegisters.r9, AssemblerRegisters.r12, AssemblerRegisters.r13] },
            { typeof(IntPtr), [AssemblerRegisters.rcx, AssemblerRegisters.rdx, AssemblerRegisters.r8, AssemblerRegisters.r9, AssemblerRegisters.r10, AssemblerRegisters.r11, AssemblerRegisters.r12, AssemblerRegisters.r13] },
            { typeof(float), [AssemblerRegisters.xmm0, AssemblerRegisters.xmm1, AssemblerRegisters.xmm2, AssemblerRegisters.xmm3, AssemblerRegisters.xmm4, AssemblerRegisters.xmm5] },
            { typeof(double), [AssemblerRegisters.xmm0, AssemblerRegisters.xmm1, AssemblerRegisters.xmm2, AssemblerRegisters.xmm3, AssemblerRegisters.xmm4, AssemblerRegisters.xmm5] },
            { typeof(char), [AssemblerRegisters.dl, AssemblerRegisters.cl, AssemblerRegisters.r8, AssemblerRegisters.r9, AssemblerRegisters.r12, AssemblerRegisters.r13] },
            { typeof(bool), [AssemblerRegisters.al, AssemblerRegisters.dl, AssemblerRegisters.cl, AssemblerRegisters.r8, AssemblerRegisters.r9, AssemblerRegisters.r12, AssemblerRegisters.r13] }
        };

        List<x64_AssemblerStep> argumentInfos = [];
        int stackOffset = 16;

        for (int i = 0; i < argumentTypes.Count; i++)
        {
            Type type = argumentTypes[i];
            object value = argumentValues[i];

            if (!availableRegisters.ContainsKey(type))
            {
                argumentInfos.Add(new x64_AssemblerStep { Instruction = x64_AssemblerStep.InstructionTarget.push, StackOffset = stackOffset });
                stackOffset += 8;
            }
            else if (type == typeof(float))
            {
                var registers = availableRegisters[type];
                foreach (var reg in registers)
                {
                    if (!argumentInfos.Exists(info => info.Register == reg))
                    {
                        argumentInfos.Add(new x64_AssemblerStep
                        {
                            Instruction = x64_AssemblerStep.InstructionTarget.mov,
                            Register = reg,
                            StackOffset = -1
                        });
                        break;
                    }
                }
            }
            else
            {
                var registers = availableRegisters[type];
                foreach (var reg in registers)
                {
                    if (!argumentInfos.Exists(info => info.Register == reg))
                    {
                        argumentInfos.Add(new x64_AssemblerStep { Instruction = x64_AssemblerStep.InstructionTarget.mov, Register = reg });
                        break;
                    }
                }

                if (!argumentInfos.Exists(info => info.Instruction == x64_AssemblerStep.InstructionTarget.mov))
                {
                    argumentInfos.Add(new x64_AssemblerStep { Instruction = x64_AssemblerStep.InstructionTarget.push, StackOffset = stackOffset });
                    stackOffset += 8;
                }
            }
        }

        return argumentInfos;
    }
}
