using System;

namespace Nova.Scripts
{
    public class DatabaseException : Exception
    {
        public DatabaseException(string message) : base(message) { }

        public DatabaseException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class DatabaseConnectionException : DatabaseException
    {
        public DatabaseConnectionException(string message) : base(message) { }

        public DatabaseConnectionException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class TableNotFoundException : DatabaseException
    {
        public TableNotFoundException(string message) : base(message) { }
        public TableNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}
