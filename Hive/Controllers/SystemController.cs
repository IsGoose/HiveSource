using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using ArmaTools.ArrayParser;
using Hive.Application;
using Hive.Application.Attributes;
using Hive.Application.Enums;
using Hive.Application.Exceptions;
using Newtonsoft.Json;

namespace Hive.Controllers
{
    public class SystemController
    {
        private static string _basePath;
        
        [Synchronous]
        public static bool Setup(bool isProduction /*Should only be True when Hive is Running from OA Server*/)
        {
            if (HiveProcess.IsSetup)
                return true;
            _basePath = AppDomain.CurrentDomain.BaseDirectory;
            
            //TODO: Implement a Proper Internal Logging Solution
            File.Delete(Path.Combine(_basePath,"HiveLog.txt"));
            File.Delete(Path.Combine(_basePath,"HiveErrorLog.txt"));

            //Load Hive Configuration from OA Server Config Directory 
            IoC.Configuration = LoadConfig(isProduction);

            if (IoC.Configuration.SpawnConsole ?? false)
                AllocConsole();

            //Instantiate on IoC Container
            IoC.HiveProcess = new HiveProcess();
            //TODO: Database Connector
            IoC.DBInterface = new DBInterface();
            IoC.DBInterface.Connect();

            //This is Up for Consideration. Schema Structure *might* be Useful at some point..
            //IoC.DBInterface.DescribeSchema();
            
            //Set Global Parser Options 
            Parser.SetThrowOnBadLooseType(true);
            
            //Must Set NumberFormatInfo Explicitly, Different Regions format Numbers Differently, Arma & Hive Will not Interface Properly with Numbers
            Parser.SetNumberFormatInfo(new NumberFormatInfo()
            {
                NumberDecimalSeparator = ".",
                NumberGroupSeparator = "",
                CurrencyDecimalSeparator = ".",
                CurrencyGroupSeparator = "",
                PercentDecimalSeparator = ".",
                PercentGroupSeparator = ""
            });
            
            HiveProcess.IsSetup = true;
            return true;
        }


        private static Configuration LoadConfig(bool isProduction)
        {
            //Load Config from Executing Directory if Hive is running from Runner
            if (!isProduction)
                return JsonConvert.DeserializeObject<Configuration>(
                    File.ReadAllText(Path.Combine(_basePath, "HiveConfig.json")));

            //Get CommandLine of Parent Server Process
            var serverProcessCmdArgs = Environment.GetCommandLineArgs();
            
            //Get -config Parameter
            var serverConfigArg = serverProcessCmdArgs.FirstOrDefault(arg =>
                arg.StartsWith("-config", StringComparison.CurrentCultureIgnoreCase));

            if (string.IsNullOrEmpty(serverConfigArg))
                throw new InvalidParameterException(
                    "Could not Acquire Server Config Parameter from OA Server CommandLine. Please Set the \"-config\" Startup Parameter for OA Server");

            //Parse -config Value and Get Config Directory 
            var serverConfigFilePath = serverConfigArg.Skip(serverConfigArg.IndexOf(':') - 1);
            if(serverConfigArg.EndsWith("\""))
                serverConfigFilePath = serverConfigFilePath.Take(serverConfigFilePath.Count() - 1);

            var parsedConfigPath = new string(serverConfigFilePath.ToArray());
            var serverConfigDirectory = string.Join("\\",parsedConfigPath
                .Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).SkipLast());

            if (!Directory.Exists(serverConfigDirectory))
                throw new DirectoryNotFoundException(
                    $"Server Config Directory \"{serverConfigDirectory}\" Does Not Exist");

            if (!File.Exists(Path.Combine(serverConfigDirectory, "HiveConfig.json")))
                throw new FileNotFoundException($"HiveConfig.json Could not Be Found in {serverConfigDirectory}\\");
            
            //Finally, get the Configuration
            return JsonConvert.DeserializeObject<Configuration>(
                File.ReadAllText(Path.Combine(serverConfigDirectory, "HiveConfig.json")));
        }
        
        [DllImport("kernel32")]
        private static extern bool AllocConsole();
    }
}