namespace ishtar.emit;

using System;
using System.Diagnostics;
using vein.runtime;

public class ExceptionBlockInfo
{
    public ExceptionBlockInfo(int ilOffset, Label label)
        => (StartAddr, EndLabel) = (ilOffset, label);

    public int StartAddr { get; protected set; }
    public int EndAddr { get; protected set; } = -1;
    public Label EndLabel { get; protected set; }
    public ExceptionBlockState State { get; set; } = ExceptionBlockState.TRY;
    public Label FinallyLabel { get; set; }
    public int EndFinally { get; set; } = -1;

    public int[] CatchAddr { get; set; } = new int[4];
    public int[] CatchEndAddr { get; set; } = new int[4];
    public int[] FilterAddr { get; set; } = new int[4];
    public VeinClass[] CatchClass { get; set; } = new VeinClass[4];
    public ExceptionMarkKind[] Types { get; set; } = new ExceptionMarkKind[4];
    public int CurrentCatch { get; set; }


    internal void Done(int endAddr)
    {
        Debug.Assert(CurrentCatch > 0);
        Debug.Assert(CatchAddr[CurrentCatch - 1] > 0);
        Debug.Assert(CatchEndAddr[CurrentCatch - 1] == -1);
        CatchEndAddr[CurrentCatch - 1] = endAddr;
        State = ExceptionBlockState.DONE;
    }

    public void MarkCatchAddr(int catchAddr, VeinClass? exception)
    {
        State = ExceptionBlockState.CATCH;
        Mark(catchAddr, catchAddr, exception, ExceptionMarkKind.NONE);
    }

    public void SetFinallyEndLabel(Label lbl)
        => FinallyLabel = lbl;

    public void MarkFaultAddr(int faultAddr)
    {
        State = ExceptionBlockState.FAULT;
        Mark(faultAddr, faultAddr, null, ExceptionMarkKind.FAULT);
    }

    public void MarkFinallyAddr(int finallyAddr, int endCatchAddr)
    {
        if (EndFinally != -1)
            throw new InvalidOperationException($"Too many finally cases");

        State = ExceptionBlockState.FINALLY;
        EndFinally = finallyAddr;
        Mark(finallyAddr, endCatchAddr, null, ExceptionMarkKind.FINALLY);
    }

    private void Mark(int filterAddr, int catchEndAddr, VeinClass? catchClass, ExceptionMarkKind type)
    {
        int currentCatch = CurrentCatch;

        if (currentCatch >= CatchAddr.Length)
        {
            FilterAddr = ILGenerator.IncreaseCapacity(FilterAddr);
            CatchAddr = ILGenerator.IncreaseCapacity(CatchAddr);
            CatchEndAddr = ILGenerator.IncreaseCapacity(CatchEndAddr);
            CatchClass = ILGenerator.IncreaseCapacity(CatchClass);
            Types = ILGenerator.IncreaseCapacity(Types);
        }
        if (type == ExceptionMarkKind.FILTER)
        {
            Types[currentCatch] = type;
            FilterAddr[currentCatch] = filterAddr;
            CatchAddr[currentCatch] = -1;
            if (currentCatch > 0)
            {
                Debug.Assert(CatchEndAddr[currentCatch - 1] == -1);
                CatchEndAddr[currentCatch - 1] = filterAddr;
            }
        }
        else
        {
            CatchClass[currentCatch] = catchClass!;
            if (Types[currentCatch] != ExceptionMarkKind.FILTER)
                Types[currentCatch] = type;
            CatchAddr[currentCatch] = filterAddr;
            if (currentCatch > 0 && Types[currentCatch] != ExceptionMarkKind.FILTER)
            {
                Debug.Assert(CatchEndAddr[currentCatch - 1] == -1);
                CatchEndAddr[currentCatch - 1] = catchEndAddr;
            }
            CatchEndAddr[currentCatch] = -1;
            CurrentCatch++;
        }

        if (EndAddr == -1) EndAddr = filterAddr;
    }

}

public enum ExceptionMarkKind
{
    NONE,
    FILTER,
    FINALLY,
    FAULT = 4,
    PREVERSE_STACK = 4
}


public enum ExceptionBlockState
{
    TRY = 0x0,
    FILTER = 0x1,
    CATCH = 0x2,
    FINALLY = 0x3,
    FAULT = 0x4,
    DONE = 0x5
}
