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
using Hive.Application.Exceptions;

namespace Hive
{
    public class RVEntry
    {
        [DllExport("_RVExtension@12", CallingConvention = CallingConvention.Winapi)]
        public static void RVExtension(StringBuilder output, int outputSize,
            [MarshalAs(UnmanagedType.LPStr)] string function)
        {
            outputSize--;
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
                    if (parameters.Length == 1)
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

            //TODO: Possibly Pass this off to background Thread, MethodInfo.Invoke is invoked on current thread
            // Or, Make this Attribute Based if the Call is Void but is intended to be blocking?
            if (isVoid)
            {
                
                success = true;
                return new ArmaString("Void Method Called Sucessfully");
            }

            //Dummy as True for Now
            if (true)
            {
                //TODO: Check for Synchronous Attribute, Invoke and Convert Result
                //Dummy as Synchronous for Now
                var result = method.Invoke(null, paramList.ToArray());
                
                //Dummy as True for Now
                success = true;
                return Convert.ChangeType(result); 
                
                
                //TODO: Check for Asynchronous Attribute (or not, shouldn't matter). Invoke Async and Return TaskID
                return new ArmaNumber(0 /*TaskID*/);
            }
            
            //TODO: Invoke Task on Background Thread and Return to RVExtension
            
            //TODO: Implement InvokeTaskAsync & InvokeTaskAndForget in to HiveProcess
            
            
            success = true;
            
            //Dummy Return
            return new ArmaArray("Hello", "World", 123, true, null, new ArmaArray(4, 5, 6));
        }
        
    }
}