using System;
using System.Text;
using Hive.Application;

namespace Runner
{
    public class HiveRunner
    {
        static void Main()
        {
            //Dummy this For Now
            HiveProcess.IsSetup = true;
            
            
            var sb = new StringBuilder();
            Hive.RVEntry.RVExtension(sb,1000,"[\"Database\",\"Sync\",[\"Dummy\",\"Parameters\",\"Here\"]]");
            Console.WriteLine(sb);

            Console.ReadLine();
        }
    }
}