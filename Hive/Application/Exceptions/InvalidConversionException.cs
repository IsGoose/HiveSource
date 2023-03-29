using System;

namespace Hive.Application.Exceptions
{
    public class InvalidConversionException : Exception
    {
        public InvalidConversionException(string message) : base (message)
        {

        }
    }
}