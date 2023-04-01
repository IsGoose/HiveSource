using System;
using System.Text;

namespace Runner
{
    public class HiveRunner
    {
        static void Main()
        {
            var sb = new StringBuilder();
            Hive.RVEntry.RVExtension(sb,1000,"[\"System\",\"Setup\",false]");
            Console.WriteLine(sb);
            Console.ReadLine();
        }
    }
}