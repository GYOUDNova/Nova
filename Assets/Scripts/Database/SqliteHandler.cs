using SQLite;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace NOVA.Scripts
{
    public abstract class SqliteHandler
    {
        protected string databaseName;
        protected string dbPath;

        protected SQLiteConnection conn;

        public SqliteHandler(string databaseName)
        {
            this.databaseName = databaseName;
        }

        // Generic initialize method to be overriden by derived classes
        protected virtual void Initialize(string directory)
        {
            // Make sure a directory exists for the database
            if (!Directory.Exists(directory))
            {
                Debug.Log($"Directory {directory} does not exist...attempting to create");

                Directory.CreateDirectory(directory);
            }
        }

        // Method to check if a table exists in the database
        public bool HasTable(string tableName)
        {
            conn = GetSqliteConnection();

            var tableInfo = conn.GetTableInfo(tableName);

            CloseConnection();

            return tableInfo != null && tableInfo.Count > 0;
        }

        // Generic method to get all objects of type T from the database
        public List<T> GetObjects<T>() where T : class, new()
        {
            conn = GetSqliteConnection();

            var tableName = typeof(T).Name;
            var tableInfo = conn.GetTableInfo(tableName);

            if (tableInfo == null || tableInfo.Count == 0)
            {
                throw new TableNotFoundException($"Table '{tableName}' not found in the database.");
            }

            // Retrieve all objects of type T from the database
            List<T> objects = conn.Table<T>().ToList();

            CloseConnection();
            return objects;
        }

        // Generic method to get an object by its ID from the database
        public T GetObjectById<T>(int id) where T : class, new()
        {
            conn = GetSqliteConnection();

            var tableName = typeof(T).Name;
            var query = $"SELECT * FROM {tableName} WHERE {tableName}Id = ?";

            T obj = conn.Query<T>(query, id).FirstOrDefault();
            if (obj == null)
            {
                throw new ItemNotFoundException($"Item with ID {id} not found in table {tableName}.");
            }

            CloseConnection();

            return obj;
        }

        // Generic method to get an object by its name from the database
        public T GetObjectByName<T>(string itemName) where T : class, new()
        {
            conn = GetSqliteConnection();
            var tableName = typeof(T).Name;

            // Check if the table contains the "Name" property
            var tableInfo = conn.GetTableInfo(tableName);
            if (tableInfo == null || !tableInfo.Any(col => col.Name == "Name"))
            {
                throw new PropertyNotFoundException($"Property 'Name' not found in table '{tableName}'.");
            }

            var query = $"SELECT * FROM {tableName} WHERE Name = ?";
            T obj = conn.Query<T>(query, itemName).FirstOrDefault();

            if (obj == null)
            {
                throw new ItemNotFoundException($"Item with name '{itemName}' not found in table '{tableName}'.");
            }

            CloseConnection();
            return obj;
        }

        // Generic add method for adding items to the database (PK is auto-incremented)
        public void AddItem<T>(T item) where T : class, new()
        {
            conn = GetSqliteConnection();

            var tableName = typeof(T).Name;
            var tableInfo = conn.GetTableInfo(tableName);

            if (tableInfo == null || tableInfo.Count == 0)
            {
                throw new TableNotFoundException($"Table '{tableName}' not found in the database.");
            }

            // Insert the new item into the database
            conn.Insert(item);
            CloseConnection();
        }

        // Add an item by name, ensuring no duplicates
        public void AddItemByName<T>(T item, string itemName) where T : class, new()
        {
            using var conn = GetSqliteConnection();
            var tableName = typeof(T).Name;

            // Check if the table exists
            var tableInfo = conn.GetTableInfo(tableName);
            if (tableInfo == null || tableInfo.Count == 0)
            {
                throw new TableNotFoundException($"Table '{tableName}' not found in the database.");
            }

            // Check if the item already exists


            var query = $"SELECT * FROM {tableName} WHERE Name = ?";
            T obj = conn.Query<T>(query, itemName).FirstOrDefault();

            if (obj != null)
            {
                throw new ItemAlreadyExistsException($"Item with name '{itemName}' already exists in table '{tableName}'.");
            }

            // Insert the new item into the database
            conn.Insert(item);
            CloseConnection();
        }

        // Method to safely close the database connection
        public void CloseConnection()
        {
            if (conn != null)
            {
                conn.Close();
                conn.Dispose();
                conn = null;
            }
        }

        // Method to get a SQLite connection.
        // Not public to ensure database operations are managed internally
        protected SQLiteConnection GetSqliteConnection()
        {
            SQLiteConnection connection;

            try
            {
                connection = new SQLiteConnection(dbPath);
            }
            catch (System.Exception)
            {
                throw new DatabaseConnectionException($"Failed to connect to the database at {dbPath}.");
            }

            return connection;
        }
    }

}
