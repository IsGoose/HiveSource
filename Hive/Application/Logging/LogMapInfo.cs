namespace Hive.Application.Logging;

public struct LogMapInfo
{
    public string FilePath;
    public int NumberOfWildcards;

    public LogMapInfo(string filePath,int numberOfWildcards)
    {
        FilePath = filePath;
        NumberOfWildcards = numberOfWildcards;
    }
}