using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using RGiesecke.DllExport;
using ArmaTools.ArrayParser;
using ArmaTools.ArrayParser.DataTypes;
using Hive.Application;
using Hive.Application.Attributes;
using Hive.Application.Exceptions;
using Hive.Application.Logging;
using Hive.Application.Logging.Internal;
using RGiesecke.DllExport;

namespace Hive
{
    public class RVEntry
    {
		public static InternalFileLogger tmp_logger = null;
        [DllExport("_RVExtension@12", CallingConvention = CallingConvention.Winapi)]
        public static void RVExtension(StringBuilder output, int outputSize,
            [MarshalAs(UnmanagedType.LPStr)] string function)
        {
			if (tmp_logger == null)
			{
				tmp_logger = new InternalFileLogger();
				tmp_logger.Initialise();
			}
            outputSize--;
            
            //TODO: Implement Internal Logging Throughout Where Applicable
            try
            {
                //Route Call to Given Controller & Method
                var result = RouteCall(function,out bool success);
                
                //Record Success & Result
                var returnResult = new ArmaArray(success);
                if(success && !(result is null))
                    returnResult.Append(result);
                
                //TODO: Check Size of Result | Max Return Limit
                //TODO: Implement Way to Retrieve Results that Exceed Max Return Limit (Return Path to .sqf File that can be Compiled)
                
                //Append Result to RVExt Return
                output.Append(returnResult);
                
            }
            catch (Exception e)
            {
                //TODO: Write Error to Log File
                Console.WriteLine(e);
				tmp_logger.Log("Error", $"{e.ToString()}", LogLevel.Error);
                Console.WriteLine("False on Throw");
                //Always Return False in Event of Thrown Exception
                output.Append(new ArmaArray(false));
            }
        }

        private static ArmaTypeBase RouteCall(string parameters, out bool success)
        {
            var entryArray = Parser.ArrayFromString(parameters);
            if (entryArray.Length < 2)
                throw new ArgumentException("Not Enough Arguments Supplied to Hive");
            var providedMethodParams = entryArray.Length > 2;

            var controllerName = entryArray.Select<ArmaString>(0).Value;
            var methodName = entryArray.Select<ArmaString>(1).Value;

            if (methodName != "Setup" && !HiveProcess.IsSetup)
                throw new ApplicationException(
                    "Hive is not Setup. Please Call the HiveController::Setup Method First.");

            var controller = Type.GetType($"Hive.Controllers.{controllerName}Controller", true, true);
            var method = controller.GetMethod(methodName,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Static);

            if (method is null)
                throw new ApplicationException($"Method: {controllerName}::{methodName} was not Found");

            var methodAttributes = method.GetCustomAttributes(false);
            var hasAttributes = methodAttributes.Length > 0;
            var methodParams = method.GetParameters();
            var methodReturnType = method.ReturnType;
            var isVoid = methodReturnType == typeof(void);
            
            var providedParameters = new ArmaArray();
            if (providedMethodParams)
            {
                providedParameters = new ArmaArray().FromList(entryArray.Elements.Skip(2).ToList());
            }
            if (methodParams.Length != providedParameters.Length)
                if(methodParams.Length == 1 && methodParams[0].ParameterType != typeof(ArmaArray))
                    throw new ArgumentException($"Argument Count Mismatch Supplied {providedParameters.Length}, Expected {methodParams.Length}");

            var paramList = new List<object>();
            foreach(var parameter in methodParams)
            {
                var type = parameter.ParameterType;
                if(type == typeof(ArmaArray))
                {
                    if (methodParams.Length == 1)
                        paramList.Add(providedParameters);
                    else
                        paramList.Add(providedParameters.SelectArray(paramList.Count));
                    continue;
                }
                if (type == typeof(bool))
                {
                    paramList.Add(providedParameters.SelectBool(paramList.Count));
                    continue;
                }
                if (type.IsNumeric())
                {
                    paramList.Add(System.Convert.ChangeType(providedParameters.SelectNumber(paramList.Count),type));
                    continue;
                }
                if (type == typeof(string))
                {
                    paramList.Add(providedParameters.SelectString(paramList.Count));
                    continue;
                }
                throw new InvalidParameterException($"Unable to Match Type \"{type.Name}\" to any ArmA-Supported Data Type (Paramater {paramList.Count})");
            }

            if (isVoid)
            {
                if (methodAttributes.Any(attrib => attrib is SynchronousAttribute))
                    method.Invoke(null, paramList.ToArray());
                else 
                    IoC.HiveProcess.InvokeTaskAndForget(method, paramList.ToArray());
                success = true;
                return new ArmaString("Void Method Called Successfully");
            }
            
            if (hasAttributes)
            {
                if (methodAttributes.Any(attrib => attrib is SynchronousAttribute))
                {
                    var synchronousResult = method.Invoke(null, paramList.ToArray());
                    success = true;
                    return Convert.ChangeType(synchronousResult); 
                }
                
                //TODO: Check for Asynchronous Attribute (?)
                var asynchronousResult = IoC.HiveProcess.InvokeTaskAsync(method, paramList.ToArray());
                success = true;
                return new ArmaNumber(asynchronousResult);
            }
            
            IoC.HiveProcess.InvokeTaskAndForget(method, paramList.ToArray());
            success = true;
            
            return new ArmaArray("Call was Fire & Forget");
        }
        
    }
}