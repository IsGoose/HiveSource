using System.Collections.Generic;
using ArmaTools.ArrayParser.DataTypes;

namespace Hive.Application.Logging;

public interface IFileLogger
{
    public abstract void Initialise();
    public abstract void Log(string alias,string log, LogLevel logLevel,ArmaArray wildcards = null);
}