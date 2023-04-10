using System;
using ArmaTools.ArrayParser;
using ArmaTools.ArrayParser.DataTypes;
using Hive.Application;
using Hive.Application.Attributes;
using Hive.Application.Enums;

namespace Hive.Controllers;

public class ExampleController
{
    /*
     * Example Table Structure: exampletable1 *
     * Column Name - DataType   Attributes
     Id -               INT PRIMARY KEY NOT NULL AUTO_INCREMENT
     ExampleString -    VARCHAR(35)
     ExampleNumber -    DOUBLE
     ExampleArray -     TEXT
     DateCreated -      DATETIME
     Deleted -          TINYINT
     DeletedAt -        DATETIME
     */
    [Synchronous]
    public static long Create(ArmaArray data)
    {
        //Data = ["ExampleString",50.25,[1,"Hello",2.5,false,"World"]]  as Received from Arma
        //So We Need to Insert Id & DateCreated, and also return Id
        data.Append(new ArmaString(DateTime.Now.ToMySqlFormat()));
        
        return System.Convert.ToInt64((IoC.DBInterface.DbInsert("exampletable1",data) as ArmaNumber).Value);
    }
    
    [Synchronous]
    public static ArmaArray GetAll()
    {
        //We Need All Entries from Table, and we Always need the Result Format as a Multi-Dim Array
        return IoC.DBInterface.DbRead("exampletable1","*","*",ArrayDimensionOptions.ForceMultiDimension);
    }
    
    [Synchronous]
    public static ArmaArray GetSingle(long id)
    {
        //We Need 1 Entry from Table. Id is Received from Arma
        return IoC.DBInterface.DbRead("exampletable1","*",$"`Id` = '{id}'");
    }
    
    public static void Update(ArmaArray data)
    {
        //We Need Update the ExampleString Column, but not any other column
        //Data = ["imagineAPrimaryKey","ExampleStringUpdated",nil,nil,nil] as Received from Arma
        IoC.DBInterface.DbUpdate("exampletable1",data);
    }

    [Synchronous]
    public static int Delete(long id)
    {
        //We Need to Delete a Row, and Return Number of Affected Rows. Id is Received from Arma
        return IoC.DBInterface.WriteRawRows($"DELETE FROM `exampletable1` WHERE `Id` = '{id}'");
    }
}