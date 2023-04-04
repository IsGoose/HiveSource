namespace Hive.Application.Logging.Internal;

public interface IInternalLogger
{
    public void Trace(string log);
    public void Debug(string log);
    public void Info(string log);
    public void Warn(string log);
    public void Error(string log);
    public void Fatal(string log);
    public void Initialise();
}