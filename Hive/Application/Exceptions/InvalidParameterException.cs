using System;

namespace Hive.Application.Exceptions
{
    public class InvalidParameterException : Exception
    {
        public InvalidParameterException(string message) : base (message)
        {

        }
    }
}