using System;

[assembly: Anotar.Custom.LogMinimalMessage]

public static class LoggerFactory
{
    public static Action<string> LogInfo { get; set; }
    public static Action<string> LogWarn { get; set; }
    public static Action<string> LogError { get; set; }

    public static Logger GetLogger<T>()
    {
        return new Logger();
    }
}

public class Logger
{
    public void Information(string format, params object[] args)
    {
        LoggerFactory.LogInfo(string.Format(format, args));
    }

    public void Information(Exception exception, string format, params object[] args)
    {
        LoggerFactory.LogInfo(string.Format(format, args) + Environment.NewLine + exception);
    }

    public bool IsInformationEnabled => LoggerFactory.LogInfo != null;

    public void Warning(string format, params object[] args)
    {
        LoggerFactory.LogWarn(string.Format(format, args));
    }

    public void Warning(Exception exception, string format, params object[] args)
    {
        LoggerFactory.LogWarn(string.Format(format, args) + Environment.NewLine + exception);
    }

    public bool IsWarningEnabled => LoggerFactory.LogWarn != null;

    public void Error(string format, params object[] args)
    {
        LoggerFactory.LogError(string.Format(format, args));
    }

    public void Error(Exception exception, string format, params object[] args)
    {
        LoggerFactory.LogError(string.Format(format, args) + Environment.NewLine + exception);
    }

    public bool IsErrorEnabled => LoggerFactory.LogError != null;
}