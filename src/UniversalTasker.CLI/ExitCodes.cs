namespace UniversalTasker.CLI;

public static class ExitCodes
{
    public const int Success = 0;
    public const int GeneralError = 1;
    public const int FileNotFound = 2;
    public const int ValidationFailed = 3;
    public const int ExecutionFailed = 4;
    public const int Interrupted = 130;
}
