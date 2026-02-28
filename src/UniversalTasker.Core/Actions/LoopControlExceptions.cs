namespace UniversalTasker.Core.Actions;

public class LoopBreakException : Exception
{
    public LoopBreakException() : base("Break statement encountered") { }
}

public class LoopContinueException : Exception
{
    public LoopContinueException() : base("Continue statement encountered") { }
}
