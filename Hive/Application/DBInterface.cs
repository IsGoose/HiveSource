using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArmaTools.ArrayParser.DataTypes;
using Hive.Application.Enums;
using MySql.Data.MySqlClient;

namespace Hive.Application
{
    public class DBInterface
    {
        private string _connectionString;
        private MySqlConnection _mySqlConnection;
        private readonly object _lock;

        private Dictionary<string, ArmaArray> _schemaStructure;

        public DBInterface()
        {
            _lock = new object();
        }

        public void Connect()
        {
            _connectionString = $"Server={IoC.Configuration.MySqlHost};Database={IoC.Configuration.MySqlSchema};Uid={IoC.Configuration.MySqlUser};Pwd={IoC.Configuration.MySqlPassword};";
            _mySqlConnection = new MySqlConnection(_connectionString);
            _mySqlConnection.Open();

            if (!_mySqlConnection.Ping())
                throw new Exception(
                    "MySQL Connection Failed, Please Ensure all Hive Configuration Settings are Correct");
            
            //TODO: Log Successful Connection
        }

        //TODO: Consider if The Idea of Storing the Schema Structure is Worth it.
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
            using (var tableReader = ReadRaw("SHOW TABLES"))
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
                using (var describeReader = ReadRaw($"DESCRIBE `{table}`"))
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
        public int WriteRaw(string command)
        {
            var affectedRows = 0;
            lock (_lock)
            {
                affectedRows = new MySqlCommand(command, _mySqlConnection).ExecuteNonQuery();
            }

            return affectedRows;
        }

        /// <summary>
        /// Alias for MySqlCommand.ExecuteReader. Use for Manually Retrieving Data When Provided Abstractions are Not Applicable
        /// </summary>
        /// <param name="command">MySQL Query to Execute</param>
        /// <returns>MySqlDataReader from Executed Query</returns>
        public MySqlDataReader ReadRaw(string command) => new MySqlCommand(command, _mySqlConnection).ExecuteReader();
    }
}