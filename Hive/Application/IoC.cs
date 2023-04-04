using Hive.Application.Logging;
using Hive.Application.Logging.Internal;

namespace Hive.Application
{
    public class IoC
    {
        public static HiveProcess HiveProcess;
        public static Configuration Configuration;
        public static DBInterface DBInterface;
        public static IInternalLogger InternalLogger;
    }
}