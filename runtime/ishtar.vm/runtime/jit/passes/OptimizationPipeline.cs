namespace ishtar.jit;

/// <summary>
/// The optimization pipeline. Runs passes in sequence, iterating until fixpoint.
/// </summary>
public static unsafe class OptimizationPipeline
{
    /// <summary>
    /// Run the standard optimization pipeline on an IR function.
    /// </summary>
    public static void Optimize(IRFunction* fn, OptLevel level)
    {
        if (level == OptLevel.None) return;

        // O1: basic passes
        RunUntilFixpoint(fn, &ConstantFoldingPass.Run);
        RunUntilFixpoint(fn, &DeadCodeEliminationPass.Run);

        if (level < OptLevel.O2) return;

        // O2: more aggressive
        RunUntilFixpoint(fn, &CommonSubexprEliminationPass.Run);
        RunUntilFixpoint(fn, &CodeMotionPass.Run);
        RunUntilFixpoint(fn, &RangeInferencingPass.Run);
        RunUntilFixpoint(fn, &BoundsCheckEliminationPass.Run);
        RunUntilFixpoint(fn, &ConstantFoldingPass.Run); // second pass after motion

        if (level < OptLevel.O3) return;

        // O3: loop optimizations
        RunUntilFixpoint(fn, &LoopUnrollingPass.Run);
        RunUntilFixpoint(fn, &AllocationSinkingPass.Run);
        RunUntilFixpoint(fn, &DeadCodeEliminationPass.Run); // cleanup
    }

    private static void RunUntilFixpoint(IRFunction* fn, delegate*<IRFunction*, bool> pass, int maxIterations = 8)
    {
        for (var i = 0; i < maxIterations; i++)
        {
            if (!pass(fn))
                break;
        }
    }
}

public enum OptLevel : byte
{
    None = 0,
    O1 = 1,
    O2 = 2,
    O3 = 3,
}
