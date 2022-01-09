namespace ishtar;

using System;

public class SkipStatementException : Exception
{
    public readonly bool IsForceStop;

    public SkipStatementException()
    {
        
    }
    public SkipStatementException(bool forceStop) => IsForceStop = forceStop;
}
