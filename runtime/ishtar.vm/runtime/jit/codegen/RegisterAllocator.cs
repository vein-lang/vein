namespace ishtar.jit;

using Iced.Intel;
using static Iced.Intel.AssemblerRegisters;

/// <summary>
/// Linear-scan register allocator for x64.
/// Maps SSA value IDs → physical registers or stack spill slots.
///
/// Available registers (callee-saved excluded from scratch pool):
///   Scratch (caller-saved): RAX, RCX, RDX, R8-R11
///   Callee-saved (if needed): RBX, R12-R15, RSI, RDI
///   Float: XMM0-XMM15 (all caller-saved on Windows, XMM0-7 on SysV)
///
/// Strategy:
///   1. Compute live intervals [start, end] for each value
///   2. Sort by start point
///   3. Greedily assign registers; on conflict, spill the longest-living value
/// </summary>
public static unsafe class RegisterAllocator
{
    // Max values we can allocate without heap — if exceeded, falls back to spilling everything
    private const int MaxValues = 4096;

    // Available integer GPRs for allocation (excluding RSP, RBP which are reserved)
    private const int ScratchIntCount = 7;  // first 7 are caller-saved
    private const int TotalIntRegs = 12;
    private const int TotalFloatRegs = 16;

    private static AssemblerRegister64 GetIntRegByIndex(int i) => i switch
    {
        0 => rax, 1 => rcx, 2 => rdx, 3 => r8, 4 => r9, 5 => r10, 6 => r11,
        7 => rbx, 8 => r12, 9 => r13, 10 => r14, 11 => r15,
        _ => rax
    };

    private static AssemblerRegisterXMM GetFloatRegByIndex(int i) => i switch
    {
        0 => xmm0, 1 => xmm1, 2 => xmm2, 3 => xmm3,
        4 => xmm4, 5 => xmm5, 6 => xmm6, 7 => xmm7,
        8 => xmm8, 9 => xmm9, 10 => xmm10, 11 => xmm11,
        12 => xmm12, 13 => xmm13, 14 => xmm14, 15 => xmm15,
        _ => xmm0
    };

    /// <summary>
    /// Allocation result for a single SSA value.
    /// </summary>
    public struct RegAlloc
    {
        /// <summary>Physical register index (into IntRegs or FloatRegs), or -1 if spilled.</summary>
        public short RegIndex;
        /// <summary>True if this is a float register.</summary>
        public bool IsFloat;
        /// <summary>Stack spill offset from RBP (negative), or 0 if in register.</summary>
        public int SpillOffset;
        /// <summary>Whether this value uses a callee-saved register (needs push/pop in prologue).</summary>
        public bool IsCalleeSaved;
    }

    /// <summary>
    /// Full allocation result for an IRFunction.
    /// </summary>
    public struct AllocResult
    {
        public RegAlloc* Allocations; // indexed by value ID
        public int ValueCount;
        public int SpillSlotCount;    // total spill slots needed
        public int CalleeSavedMask;   // bitmask of callee-saved regs used

        /// <summary>Total frame size needed for spills (aligned to 16).</summary>
        public int SpillFrameSize => ((SpillSlotCount * 8) + 15) & ~15;
    }

    public static AllocResult Allocate(IRFunction* fn)
    {
        var valueCount = fn->ValueCount;
        var result = new AllocResult();
        result.ValueCount = valueCount;
        result.Allocations = (RegAlloc*)fn->Allocator.alloc((uint)(valueCount * sizeof(RegAlloc)));
        result.SpillSlotCount = 0;
        result.CalleeSavedMask = 0;

        // Initialize all to unallocated
        for (var i = 0; i < valueCount; i++)
        {
            result.Allocations[i].RegIndex = -1;
            result.Allocations[i].SpillOffset = 0;
        }

        // Step 1: Compute live intervals
        var intervals = stackalloc LiveInterval[valueCount > MaxValues ? 1 : valueCount];
        if (valueCount > MaxValues)
        {
            // Too many values — spill everything
            SpillAll(fn, &result);
            return result;
        }

        ComputeLiveIntervals(fn, intervals);

        // Step 2: Sort values by start point (insertion sort for simplicity with unmanaged)
        var order = stackalloc int[valueCount];
        for (var i = 0; i < valueCount; i++) order[i] = i;
        InsertionSort(order, intervals, valueCount);

        // Step 3: Linear scan allocation
        var intRegFreeUntil = stackalloc int[TotalIntRegs];
        var floatRegFreeUntil = stackalloc int[TotalFloatRegs];
        for (var i = 0; i < TotalIntRegs; i++) intRegFreeUntil[i] = 0;
        for (var i = 0; i < TotalFloatRegs; i++) floatRegFreeUntil[i] = 0;

        for (var idx = 0; idx < valueCount; idx++)
        {
            var valId = order[idx];
            var interval = &intervals[valId];
            if (interval->Start < 0) continue; // dead value

            var isFloat = IRTypeMap.IsFloat(fn->Values[valId].Type);

            if (isFloat)
            {
                var reg = FindFreeReg(floatRegFreeUntil, TotalFloatRegs, interval->Start);
                if (reg >= 0)
                {
                    result.Allocations[valId].RegIndex = (short)reg;
                    result.Allocations[valId].IsFloat = true;
                    floatRegFreeUntil[reg] = interval->End;
                }
                else
                {
                    Spill(fn, &result, valId);
                }
            }
            else
            {
                var reg = FindFreeReg(intRegFreeUntil, TotalIntRegs, interval->Start);
                if (reg >= 0)
                {
                    result.Allocations[valId].RegIndex = (short)reg;
                    result.Allocations[valId].IsFloat = false;
                    result.Allocations[valId].IsCalleeSaved = reg >= ScratchIntCount;
                    if (reg >= ScratchIntCount)
                        result.CalleeSavedMask |= (1 << reg);
                    intRegFreeUntil[reg] = interval->End;
                }
                else
                {
                    Spill(fn, &result, valId);
                }
            }
        }

        return result;
    }

    private struct LiveInterval
    {
        public int Start; // first instruction position
        public int End;   // last use position
    }

    private static void ComputeLiveIntervals(IRFunction* fn, LiveInterval* intervals)
    {
        var valueCount = fn->ValueCount;

        for (var i = 0; i < valueCount; i++)
        {
            intervals[i].Start = -1;
            intervals[i].End = -1;
        }

        // Walk all instructions linearly, assign position numbers
        var pos = 0;
        for (var b = 0; b < fn->BlockCount; b++)
        {
            var block = &fn->Blocks[b];
            var instrCount = block->Instructions->Count;

            for (var i = 0; i < instrCount; i++)
            {
                var instrId = block->Instructions->Get(i)->Id;
                var instr = &fn->Instructions[instrId];
                if (instr->IsDead) { pos++; continue; }

                // Define
                if (instr->ResultId >= 0 && instr->ResultId < valueCount)
                {
                    if (intervals[instr->ResultId].Start < 0)
                        intervals[instr->ResultId].Start = pos;
                }

                // Uses
                for (var j = 0; j < instr->OperandCount; j++)
                {
                    var opId = instr->GetOperand(j);
                    if (opId >= 0 && opId < valueCount)
                    {
                        intervals[opId].End = pos;
                        if (intervals[opId].Start < 0)
                            intervals[opId].Start = 0; // defined externally (arg)
                    }
                }

                pos++;
            }
        }

        // Ensure every defined value has at least a point interval
        for (var i = 0; i < valueCount; i++)
        {
            if (intervals[i].Start >= 0 && intervals[i].End < intervals[i].Start)
                intervals[i].End = intervals[i].Start;
        }
    }

    private static int FindFreeReg(int* freeUntil, int regCount, int currentPos)
    {
        for (var i = 0; i < regCount; i++)
        {
            if (freeUntil[i] <= currentPos)
                return i;
        }
        return -1; // no free reg
    }

    private static void Spill(IRFunction* fn, AllocResult* result, int valueId)
    {
        result->SpillSlotCount++;
        result->Allocations[valueId].RegIndex = -1;
        result->Allocations[valueId].SpillOffset = -(result->SpillSlotCount * 8);
    }

    private static void SpillAll(IRFunction* fn, AllocResult* result)
    {
        for (var i = 0; i < fn->ValueCount; i++)
        {
            result->SpillSlotCount++;
            result->Allocations[i].RegIndex = -1;
            result->Allocations[i].SpillOffset = -(result->SpillSlotCount * 8);
        }
    }

    private static void InsertionSort(int* order, LiveInterval* intervals, int count)
    {
        for (var i = 1; i < count; i++)
        {
            var key = order[i];
            var keyStart = intervals[key].Start;
            var j = i - 1;
            while (j >= 0 && intervals[order[j]].Start > keyStart)
            {
                order[j + 1] = order[j];
                j--;
            }
            order[j + 1] = key;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Public helpers for codegen to resolve allocations
    // ═══════════════════════════════════════════════════════════════════

    public static AssemblerRegister64 GetIntReg(int regIndex) => GetIntRegByIndex(regIndex);
    public static AssemblerRegisterXMM GetFloatReg(int regIndex) => GetFloatRegByIndex(regIndex);
    public static int GetCalleeSavedStart() => ScratchIntCount;
    public static int GetTotalIntRegs() => TotalIntRegs;
}
