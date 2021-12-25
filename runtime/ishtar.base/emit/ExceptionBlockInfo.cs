namespace ishtar.emit;

using System;
using System.Diagnostics;
using vein.runtime;

public class ExceptionBlockInfo
{
    public ExceptionBlockInfo(int ilOffset, Label label)
        => (StartAddr, EndLabel) = (ilOffset, label);

    public ExceptionBlockInfo() {}

    public int StartAddr { get; internal set; }
    public int EndAddr { get; internal set; } = -1;
    public Label EndLabel { get; protected set; }
    public ExceptionBlockState State { get; set; } = ExceptionBlockState.TRY;
    public Label FinallyLabel { get; set; }
    public int EndFinally { get; set; } = -1;

    public int[] CatchAddr { get; set; } = new int[4];
    public int[] FilterAddr { get; set; } = new int[4];
    public VeinClass[] CatchClass { get; set; } = new VeinClass[4];
    public ExceptionMarkKind[] Types { get; set; } = new ExceptionMarkKind[4];
    public int CurrentCatch { get; set; }


    internal void Done(int endAddr)
    {
        Debug.Assert(CurrentCatch > 0);
        State = ExceptionBlockState.DONE;
    }

    public void MarkCatchAddr(int catchAddr, VeinClass? exception)
    {
        State = ExceptionBlockState.CATCH;
        if (exception is not null)
            Mark(catchAddr, exception, ExceptionMarkKind.FILTER);
        else
            Mark(catchAddr, null, ExceptionMarkKind.CATCH_ANY);
    }

    public void SetFinallyEndLabel(Label lbl)
        => FinallyLabel = lbl;

    public void MarkFinallyAddr(int finallyAddr)
    {
        if (EndFinally != -1)
            throw new InvalidOperationException($"Too many finally cases");

        State = ExceptionBlockState.FINALLY;
        EndFinally = finallyAddr;
        Mark(finallyAddr, null, ExceptionMarkKind.FINALLY);
    }

    private void Mark(int addr, VeinClass? catchClass, ExceptionMarkKind type)
    {
        int currentCatch = CurrentCatch;

        if (currentCatch >= CatchAddr.Length)
        {
            FilterAddr = ILGenerator.IncreaseCapacity(FilterAddr);
            CatchAddr = ILGenerator.IncreaseCapacity(CatchAddr);
            CatchClass = ILGenerator.IncreaseCapacity(CatchClass);
            Types = ILGenerator.IncreaseCapacity(Types);
        }

        if (type == ExceptionMarkKind.FILTER)
        {
            Types[currentCatch] = type;
            FilterAddr[currentCatch] = addr;
            CatchAddr[currentCatch] = -1;
            CatchClass[currentCatch] = catchClass;
        }
        else if (type == ExceptionMarkKind.FINALLY)
        {
            Types[currentCatch] = type;
            FilterAddr[currentCatch] = -1;
            CatchAddr[currentCatch] = -1;
            CatchClass[currentCatch] = VeinCore.VoidClass;
        }
        CurrentCatch++;
        if (EndAddr == -1) EndAddr = addr;
    }
}

public enum ExceptionMarkKind : byte
{
    NONE,
    FILTER,
    CATCH_ANY,
    FINALLY
}


public enum ExceptionBlockState : byte
{
    TRY = 0x0,
    FILTER = 0x1,
    CATCH = 0x2,
    FINALLY = 0x3,
    DONE = 0x4
}
