﻿using System;
using System.Collections.Generic;
using ArmaTools.ArrayParser;
using ArmaTools.ArrayParser.DataTypes;
using Hive.Application.Exceptions;

namespace Hive
{
    public static class Extensions
    {
        public static string ToMySqlFormat(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
        }
        
        //TODO: Move to ArrayParser Natively
        public static ArmaArray FromList(this ArmaArray array, List<ArmaTypeBase> list)
        {
            var result = new ArmaArray();
            if (list[0] is ArmaArray)
                result = list[0] as ArmaArray;
            else
                result.Append(list[0]);
            
            return result;
        }
        
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source)
        {
            using var e = source.GetEnumerator();
            if (!e.MoveNext()) yield break;
            for (var value = e.Current; e.MoveNext(); value = e.Current)
            {
                yield return value;
            }
        }

        public static string Format(this string input, ArmaArray parameters)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                var element = parameters.Elements[i];
                input = input.Replace($"{{{i}}}", element is ArmaString armaString ? armaString.Value : element.ToString());
            }

            return input;
        }
    }

    public static class Convert
    {
        public static ArmaTypeBase ChangeType(object value)
        {
            var valueType = value.GetType();
            try
            {
                
                if (value is ArmaTypeBase)
                    return value as ArmaTypeBase;
                if (valueType.IsNumeric())
                    return new ArmaNumber(System.Convert.ToDouble(value,Parser.NumberFormatInfo));
                if (valueType == typeof(string))
                    return new ArmaString(value.ToString());
                if (valueType == typeof(bool))
                    return new ArmaBool(System.Convert.ToBoolean(value));
            }
            catch
            {
                throw new InvalidConversionException($"Unable to Convert Type: {valueType.Name} to an Arma-Compatible Data Type");
            }
            throw new InvalidConversionException($"Unable to Convert Type: {valueType.Name} to an Arma-Compatible Data Type");
        }
    }
}