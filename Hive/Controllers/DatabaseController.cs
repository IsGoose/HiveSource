using ArmaTools.ArrayParser.DataTypes;

namespace Hive.Controllers
{
    public class DatabaseController
    {
        public static ArmaArray Sync(string param1, string param2, string param3)
        {
            return new ArmaArray(param3, param2, param1);
        }
    }
}