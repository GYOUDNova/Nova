using SQLite;
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

        protected virtual void Initialize(string directory)
        {
            // Make sure a directory exists for the database
            if (!Directory.Exists(directory))
            {
                Debug.Log($"Directory {directory} does not exist...attempting to create");

                Directory.CreateDirectory(directory);
            }
        }
        public bool HasTable(string tableName)
        {
            conn = GetSqliteConnection();

            var tableInfo = conn.GetTableInfo(tableName);

            CloseConnection();

            return tableInfo != null;
        }

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

        public void CloseConnection()
        {
            if (conn != null)
            {
                conn.Close();
                conn.Dispose();
                conn = null;
            }
        }

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
