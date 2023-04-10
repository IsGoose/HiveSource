using System;
using System.Globalization;
using System.IO;
using System.Linq;
using ArmaTools.ArrayParser;
using Hive.Application;
using Hive.Application.Attributes;
using Hive.Application.Exceptions;
using Hive.Application.Extern;
using Hive.Application.Logging;
using Hive.Application.Logging.Internal;
using MySql.Data.MySqlClient;
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

			try
			{
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
				IoC.InternalLogger.Info("System Setup Completed. Hive is Ready");
				return true;
			}
			catch (Exception e)
			{
				//Crash Server Process & Display MessageBox With Internal Exception Info
				var crashMessage = e switch
				{
					JsonException jsonException =>
						"Error Parsing JSON Configuration. Please Make Sure HiveConfig.json is Correct",
					IndexOutOfRangeException indexOORException => "GameLogMap Config Option is Invalid",
					IOException ioException => "IO Error. Please Make Sure File Paths Set in HiveConfig.json Are Correct and are Valid",
					InvalidOperationException invalidOperationException => "Invalid Operation",
					MySqlException mySqlException => "Error When Interfacing with MySql. Please Make Sure MySql is Running, and MySql Settings are Correct in HiveConfig.json",
					_ => "Generic Error"
				};
				
				#if DEBUG
				crashMessage = $"{crashMessage}{Environment.NewLine}Exception: {e}";
				#else
				crashMessage = $"{crashMessage}{Environment.NewLine}Message: {e.Message}";
				#endif
				
				
				Win32.MessageBox(IntPtr.Zero, crashMessage, "Internal Hive Error",
					Win32.MB_ICONERROR | Win32.MB_OK);
				Win32.ExitProcess(1);
				
				return false;
			}
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
			var serverConfigFilePath = new string(serverConfigArg.Skip(serverConfigArg.IndexOf('=') + 1).ToArray());
			serverConfigFilePath = serverConfigFilePath.Trim('\\',' ');

			var serverConfigDirectory = "";
			
			if (!File.Exists(Path.Combine(_basePath, serverConfigFilePath)))
			{
				//-config is absolute
				serverConfigDirectory = Path.GetDirectoryName(serverConfigFilePath);
				if (string.IsNullOrEmpty(serverConfigDirectory) ||
				    serverConfigDirectory.IndexOfAny(Path.GetInvalidPathChars()) >= 0 ||
				    !Directory.Exists(serverConfigDirectory))
					throw new DirectoryNotFoundException(
						$"Server Config Directory \"{serverConfigDirectory}\" Does Not Exist");
			

				if (!File.Exists(Path.Combine(serverConfigDirectory, "HiveConfig.json")))
					throw new FileNotFoundException($"HiveConfig.json Could not Be Found in {serverConfigDirectory}\\");
			}
			else
			{
				//-config is relative
				serverConfigDirectory = Path.GetDirectoryName(Path.Combine(_basePath, serverConfigFilePath));
				if (string.IsNullOrEmpty(serverConfigDirectory) ||
				    serverConfigDirectory.IndexOfAny(Path.GetInvalidPathChars()) >= 0 ||
				    !Directory.Exists(serverConfigDirectory))
					throw new DirectoryNotFoundException(
						$"Server Config Directory \"{serverConfigDirectory}\" Does Not Exist");
			}
			
			//Finally, get the Configuration
			try
			{
				return JsonConvert.DeserializeObject<Configuration>(
					File.ReadAllText(Path.Combine(serverConfigDirectory, "HiveConfig.json")));
			}
			catch (Exception e)
			{
				throw e;
			}
			
		}
	}
}