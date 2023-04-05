using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using ArmaTools.ArrayParser.DataTypes;
using Hive.Application.Enums;
using Hive.Application.Exceptions;
using MySql.Data.MySqlClient;

namespace Hive.Application
{
    public class DBInterface
    {
        private MySqlConnectionStringBuilder _connectionString;
        private readonly object _lock;

        private Dictionary<string, ArmaArray> _schemaStructure;

        public DBInterface()
        {
            _lock = new object();
        }

        public void Connect()
        {
            _connectionString = new MySqlConnectionStringBuilder()
            {
                Server = IoC.Configuration.MySqlHost,
                Database = IoC.Configuration.MySqlSchema,
                UserID = IoC.Configuration.MySqlUser,
                Password = IoC.Configuration.MySqlPassword,
                ConnectionLifeTime = 21600,
                ConnectionTimeout = 1,
                MinimumPoolSize = 10
            };
            var _mySqlConnection = new MySqlConnection(_connectionString.GetConnectionString(true));

            _mySqlConnection.Open();

            if (!_mySqlConnection.Ping())
                throw new Exception(
                    "MySQL Connection Failed, Please Ensure all Hive Configuration Settings are Correct");
            
            //TODO: Log Successful Connection
        }

        #region Database Abstractions

        /// <summary>
        /// Abstraction for Reading Data from a Table
        /// </summary>
        /// <param name="targetTable">Name of the Table to Query</param>
        /// <param name="columns">Columns to Select. Follows Standard MySQL SELECT Syntax</param>
        /// <param name="pattern">Select Condition. IE `WHERE`. Follows Standard MySQL WHERE Syntax</param>
        /// <param name="arrayDimensionOptions">Return Array format</param>
        /// <returns>Read Result(s)</returns>
        public ArmaArray DbRead(string targetTable, string columns = "*", string pattern = "*",ArrayDimensionOptions arrayDimensionOptions = ArrayDimensionOptions.None)
        {
            //Construct MySQL Query
            var baseQuery = new StringBuilder($"SELECT {columns} FROM `{targetTable}`");

            if (pattern != "*")
                baseQuery.Append($"WHERE {pattern}");

            var result = new ArmaArray();
            var readCount = 0;
            using var connection = new MySqlConnection(_connectionString.GetConnectionString(true));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = baseQuery.ToString();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var dbData = new List<object>();
                var isDeleted = false;

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnData = reader[i];
                    var columnName = reader.GetName(i);
                    //Check if Row is Marked as Deleted
                    if (columnName == "Deleted" && (sbyte)columnData == 1)
                    {
                        isDeleted = true;
                        break;
                    }

                    //Handle Column Data and Push to List
                    if (columnData == DBNull.Value)
                    {
                        dbData.Add(null);
                        continue;
                    }

                    var dataType = reader.GetDataTypeName(i);
                    
                    if(dataType == "TINYINT" && columnData.GetType() != typeof(bool))
                        dbData.Add((sbyte) columnData == 1);
                    else 
                        dbData.Add(columnData);

                }
                
                //Skip Appending Row to Result Array if Marked for Deletion
                if(isDeleted) continue;
                
                //Change how Results are Pushed to Final Array based on Given Option
                if(arrayDimensionOptions == ArrayDimensionOptions.MultiToSingle)
                    result.AppendRange(new ArmaArray(dbData.ToArray()));
                else
                    result.Append(new ArmaArray(dbData.ToArray()));
                readCount++;
            }

            //Return a Single Result as Single-Dim Array if only 1 Row was Read and No Options Provided
            if (readCount == 1 && arrayDimensionOptions == ArrayDimensionOptions.None)
                result = result.SelectArray(0);

                return result;
        }

        /// <summary>
        /// Updates Row with the Data Provided in the Input Array, where the First Element of the Array Matches the Primary Key of Target Table 
        /// </summary>
        /// <param name="targetTable">Target Table</param>
        /// <param name="rowData">Data Array</param>
        /// <exception cref="KeyNotFoundException">Thrown When Target Table was Not Found in the Schema</exception>
        /// <exception cref="InvalidOperationException">Thrown When Input Array is Either Empty, or First Element is Not String</exception>
        /// <exception cref="ArgumentException">Thrown When Length of Input Array and Number of Columns in Target Do Not Match</exception>
        /// <exception cref="InvalidParameterException">Thrown When First Column in Target Table is Not VARCHAR & PrimaryKey</exception>
        public void DbUpdate(string targetTable, ArmaArray rowData)
        {
            //Check Schema Contains Target Table
            if (!_schemaStructure.ContainsKey(targetTable.ToLower()))
                throw new KeyNotFoundException(
                    $"The Target Table `{targetTable}` was Not Found in Schema `{IoC.Configuration.MySqlSchema}` for DbUpdate");

            if (rowData.Length == 0)
                throw new InvalidOperationException("Array to Update to Table has no Elements");
            if (rowData[0] is not ArmaString)
                throw new InvalidOperationException("First Element of Array Must be a String for DbUpdate Primary Key");
            
            //Check if Table Has Soft Deletion Implemented
            var targetTableStructure = _schemaStructure[targetTable.ToLower()];
            var tableColumnCount = targetTableStructure.Length;
            var tableHasSoftDeletion = tableColumnCount > 2 &&
                                       targetTableStructure.SelectArray(tableColumnCount - 2).SelectString(0) ==
                                       "Deleted";
            
            if (tableHasSoftDeletion)
                tableColumnCount -= 2;

            //Check Input Array Length Against Table Column Count
            if(rowData.Length != tableColumnCount)
                throw new ArgumentException($"Argument Count Mismatch Supplied {rowData.Length}, Expected {tableColumnCount}. If Table Implements Soft Deletion, Please Omit Values for these From the Input Array");
            
            //Check that First Column of Table is Primary Key AND String
            if (!targetTableStructure.SelectArray(0).SelectString(1)
                    .StartsWith("varchar", StringComparison.OrdinalIgnoreCase) ||
                !targetTableStructure.SelectArray(0).SelectBool(2))
                throw new InvalidParameterException(
                    $"The First Column in Table `{targetTable}` Must Be VARCHAR(36) And Marked as Primary Key");
            
            //Match Column to Input Data. Omit Primary Key from Statement
            var valuesTable = new Dictionary<string, string>();
            var dataSets = new List<string>();
            for (int i = 1; i < rowData.Length; i++)
            {
                var value = rowData[i];
                
                //Skip Updating Column if Null
                if(value is null or ArmaNull)
                    continue;

                var field = targetTableStructure.SelectArray(i).SelectString(0);

                //ArmaArray Must be Wrapped in ' '
                dataSets.Add(value is ArmaArray ? $"`{field}` = '{value}'" : $"`{field}` = {value}");
            }
            
            //Revert Soft Deletion if Configured to Do So 
            if (tableHasSoftDeletion && (IoC.Configuration.UpdateRevertsSoftDeletion ?? false))
            {
                dataSets.Add("`Deleted` = 0");
                dataSets.Add("`DeletedAt` = NULL");
            }

            //Finally, Construct MySQL Statement
            var primaryKey = rowData.SelectString(0);
            var statement =
                $"UPDATE `{targetTable}` SET {string.Join(",", dataSets)} WHERE `{targetTableStructure.SelectArray(0).SelectString(0)}` = '{primaryKey}'";

            //Execute Statement
            WriteRaw(statement);
        }

        /// <summary>
        /// Inserts Given Array in to Target Table
        /// </summary>
        /// <param name="targetTable">Target Table</param>
        /// <param name="rowData">Data Array</param>
        /// <exception cref="KeyNotFoundException">Thrown When Target Table was Not Found in the Schema</exception>
        /// <exception cref="InvalidOperationException">Thrown When Input Array is Either Empty, or First Element is Not String</exception>
        /// <exception cref="ArgumentException">Thrown When Length of Input Array and Number of Columns in Target Do Not Match</exception>
        /// <exception cref="InvalidParameterException">Thrown When First Column in Target Table is Not VARCHAR & PrimaryKey</exception>
        public void DbInsert(string targetTable, ArmaArray rowData)
        {
            //Check Schema Contains Target Table
            if (!_schemaStructure.ContainsKey(targetTable.ToLower()))
                throw new KeyNotFoundException(
                    $"The Target Table `{targetTable}` was Not Found in Schema `{IoC.Configuration.MySqlSchema}` for DbInsert");

            if (rowData.Length == 0)
                throw new InvalidOperationException("Array to Insert into Table has no Elements");
            if (rowData[0] is not ArmaString)
                throw new InvalidOperationException("First Element of Array Must be a String for DbInsert Primary Key");
            
            //Check if Table Has Soft Deletion Implemented
            var targetTableStructure = _schemaStructure[targetTable.ToLower()];
            var tableColumnCount = targetTableStructure.Length;
            var tableHasSoftDeletion = tableColumnCount > 2 &&
                                       targetTableStructure.SelectArray(tableColumnCount - 2).SelectString(0) ==
                                       "Deleted";
            
            if (tableHasSoftDeletion)
                tableColumnCount -= 2;

            //Check Input Array Length Against Table Column Count
            if(rowData.Length != tableColumnCount)
                throw new ArgumentException($"Argument Count Mismatch Supplied {rowData.Length}, Expected {tableColumnCount}. If Table Implements Soft Deletion, Please Omit Values for these From the Input Array");
            
            //Check that First Column of Table is Primary Key AND String
            if (!targetTableStructure.SelectArray(0).SelectString(1)
                    .StartsWith("varchar", StringComparison.OrdinalIgnoreCase) ||
                !targetTableStructure.SelectArray(0).SelectBool(2))
                throw new InvalidParameterException(
                    $"The First Column in Table `{targetTable}` Must Be VARCHAR(36) And Marked as Primary Key");
            
            //Match Column to Input Data
            var dataSets = new Dictionary<string, string>();
            for (int i = 0; i < rowData.Length; i++)
            {
                var fieldName = targetTableStructure.SelectArray(i).SelectString(0);
                var value = rowData[i];
                
                if(value is null or ArmaNull)
                {
                    dataSets.Add(fieldName,"NULL");
                    continue;
                }

                //ArmaArray Must be Wrapped in ' '
                dataSets.Add(fieldName,value is ArmaArray ? $"'{value}'" : $"{value}");
            }
            
            //Set Soft Deletion Flags
            if (tableHasSoftDeletion)
            {
                dataSets.Add("Deleted","0");
                dataSets.Add("DeletedAt","NULL");
            }

            //Finally, Construct MySQL Statement
            var statement = $"INSERT INTO `{targetTable}` ({string.Join(",",dataSets.Keys)}) VALUES ({string.Join(",",dataSets.Values)})";

            //Execute Statement
            WriteRaw(statement);
        }
        #endregion
        
        
        /// <summary>
        /// Queries MySQL Server to Get All Tables &amp; Their Structure's within the Current Schema. This Method Should only Ever be Called Once on Hive Setup
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown When Method Gets Called More than Once</exception>
        public void DescribeSchema()
        {
            if (_schemaStructure is not null)
                throw new InvalidOperationException("DBInterface.DescribeSchema() Should Only be Called Once");

            _schemaStructure = new Dictionary<string, ArmaArray>();
            var tableNames = new List<string>();
            
            //Read Table Names in Schema
            using var connection = new MySqlConnection(_connectionString.GetConnectionString(true));
            using (var tableReader = ReadRaw("SHOW TABLES",connection))
            {
                while (tableReader.Read())
                {
                    //Unfortunately, we cannot do this all at once, as 1 MySqlConnection cannot have 2 Open Readers at once.
                    tableNames.Add(tableReader[0].ToString());
                }
            }

            foreach (var table in tableNames)
            {
                var tableStructure = new ArmaArray();
                using (var describeReader = ReadRaw($"DESCRIBE `{table}`",connection))
                {
                    while (describeReader.Read())
                    {
                        var fieldName = describeReader["Field"].ToString();
                        var dataType = describeReader["Type"].ToString();
                        var isPrimaryKey = describeReader["Key"].ToString() == "PRI";
                        tableStructure.Append(new ArmaArray(fieldName, dataType, isPrimaryKey));
                    }
                }

                _schemaStructure.Add(table.ToLower(), tableStructure);
            }
        }

        /// <summary>
        /// Executes Raw MySQL Query. Use for Updating Individual Columns or When Provided Abstractions are Not Applicable
        /// </summary>
        /// <param name="command">MySQL Query to Execute</param>
        /// <returns>Number of Affected Rows</returns>
        public int WriteRaw(string statement)
        {
            var affectedRows = 0;
            using var connection = new MySqlConnection(_connectionString.GetConnectionString(true));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = statement;
            affectedRows = command.ExecuteNonQuery();

            return affectedRows;
        }

        /// <summary>
        /// Alias for MySqlCommand.ExecuteReader. Use for Manually Retrieving Data When Provided Abstractions are Not Applicable
        /// </summary>
        /// <param name="command">MySQL Query to Execute</param>
        /// <returns>MySqlDataReader from Executed Query</returns>
        public MySqlDataReader ReadRaw(string statement,MySqlConnection connection)
        {

            if(connection.State != ConnectionState.Open)
                connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = statement;
            return command.ExecuteReader();
        } 
    }
}