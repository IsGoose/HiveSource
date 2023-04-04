using System;
using System.Globalization;
using System.IO;
using System.Linq;
using ArmaTools.ArrayParser;
using Hive.Application;
using Hive.Application.Attributes;
using Hive.Application.Exceptions;
using Hive.Application.Logging;
using Hive.Application.Logging.Internal;
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

			//TODO: Should SystemController:Setup Fail At Any Point, Throw Fatal Error and Exit Process

			//Load Hive Configuration from OA Server Config Directory 
			IoC.Configuration = LoadConfig(isProduction);

			//Internal Logging for Current Session (Separate Console or Server Console Window)
			if (IoC.Configuration.UseExternalConsole || !isProduction)
				IoC.InternalLogger = new ConsoleLogger();
			else
				IoC.InternalLogger = new ProcessLogger();

			IoC.InternalLogger.Initialise();
			IoC.FileLogger = new FileLogger();
			IoC.FileLogger.Initialise();

			//Test Logs
			IoC.InternalLogger.Trace("This is a Trace Log");
			IoC.InternalLogger.Debug("This is a Debug Log");
			IoC.InternalLogger.Info("This is an Info Log");
			IoC.InternalLogger.Warn("This is a Warn Log");
			IoC.InternalLogger.Error("This is an Error Log");
			IoC.InternalLogger.Fatal("This is a Fatal Log");

			//TODO: Implement LogController for Logging from Server

			IoC.HiveProcess = new HiveProcess(isProduction);

			IoC.DBInterface = new DBInterface();
			IoC.DBInterface.Connect();
			IoC.DBInterface.DescribeSchema();

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
			{
				throw new InvalidParameterException(
				"Could not Acquire Server Config Parameter from OA Server CommandLine. Please Set the \"-config\" Startup Parameter for OA Server");
			}

			//Parse -config Value and Get Config Directory 
			string serverConfigFilePath = new String(serverConfigArg.Skip(serverConfigArg.IndexOf(':') - 1).ToArray());
			if (serverConfigArg.EndsWith("\""))
				serverConfigFilePath = serverConfigFilePath.Take(serverConfigFilePath.Count() - 1).ToString();
			if (serverConfigFilePath.StartsWith("-config="))
				serverConfigFilePath = serverConfigFilePath.Replace("-config=", String.Empty);

			var parsedConfigPath = new string(serverConfigFilePath.ToArray());

			var serverConfigDirectory = string.Join("\\", parsedConfigPath
				.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).SkipLast());
			serverConfigDirectory = Path.Combine(_basePath, serverConfigDirectory);

			if (!Directory.Exists(serverConfigDirectory))
				throw new DirectoryNotFoundException(
					$"Server Config Directory \"{serverConfigDirectory}\" Does Not Exist");

			if (!File.Exists(Path.Combine(serverConfigDirectory, "HiveConfig.json")))
				throw new FileNotFoundException($"HiveConfig.json Could not Be Found in {serverConfigDirectory}\\");

			//Finally, get the Configuration
			return JsonConvert.DeserializeObject<Configuration>(
				File.ReadAllText(Path.Combine(serverConfigDirectory, "HiveConfig.json")));
		}
	}
}