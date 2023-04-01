using System;
using System.Text;
using ArmaTools.ArrayParser;
using ArmaTools.ArrayParser.DataTypes;
using Hive;

namespace Runner
{
    public class HiveRunner
    {
        static void Main()
        {
            if (!Setup())
                throw new Exception("Hive Failed to Setup!");
            
            HiveRunnerExample();
            
            Console.WriteLine();
            Console.WriteLine("Press Enter to Exit..");
            Console.ReadLine();

            
        }
        
        static ArmaArray SendToExtension(string controller, string method, ArmaTypeBase parameters)
        {
            try
            {
                var result = new StringBuilder();
                RVEntry.RVExtension(result,10240,$"[\"{controller}\",\"{method}\",{parameters}]");
                return Parser.ArrayFromString(result.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new ArmaArray(false);
            }
            
        }

        static bool Setup()
        {
            var result = SendToExtension("System", "Setup", new ArmaBool(false));
            Console.WriteLine($"Setup Result: {result}");
            return result.SelectBool(0);
        }

        static void HiveRunnerExample()
        {
            Console.WriteLine();
            Console.WriteLine("Press Any Key to Insert a Row..");
            Console.ReadKey();
            var createResult = SendToExtension("Example", "Create",
                new ArmaArray("ExampleString", 50.25, new ArmaArray(1, "Hello", 2.5, false, "World")));
            var createResultId = createResult.SelectString(1);
            Console.WriteLine($"Created Id: {createResultId}");

            Console.WriteLine();
            Console.WriteLine("Press Any Key to Get Data From the Id..");
            Console.ReadKey();
            var singleData = SendToExtension("Example", "GetSingle", new ArmaString(createResultId));
            Console.WriteLine("Data From GetSingle Call:");
            Console.WriteLine(singleData.SelectArray(1));

            Console.WriteLine();
            Console.WriteLine("Press Any Key to Create Another Row..");
            Console.ReadKey();
            var createResult2 = SendToExtension("Example", "Create",
                new ArmaArray("ExampleString2", 25.50, new ArmaArray(1, "Hello", 5.2, true, "Again")));
            Console.WriteLine($"Created Id: {createResult2.SelectString(1)}");

            Console.WriteLine();
            Console.WriteLine("Press Any Key to Update the First Row we Inserted..");
            Console.ReadKey();
            
            SendToExtension("Example", "Update",
                new ArmaArray(createResultId,"ExampleStringUpdated", null, null,null));

            Console.WriteLine();
            Console.WriteLine("Press Any Key to Get All Rows..");
            Console.ReadKey();
            var getAllRowsResult = SendToExtension("Example", "GetAll",new ArmaArray());
            Console.WriteLine(getAllRowsResult.SelectArray(1));
            
            Console.WriteLine("Press Any Key to Delete the Second Row we Inserted..");
            Console.ReadKey();
            var deleteResult = SendToExtension("Example", "Delete",new ArmaString(createResult2.SelectString(1)));
            Console.WriteLine($"Rows Affected: {deleteResult.SelectNumber(1)}");

        }
    }
}