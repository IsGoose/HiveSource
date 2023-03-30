using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Hive.Application;

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