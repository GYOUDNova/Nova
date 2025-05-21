using System;

namespace NOVA.Scripts
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

    public class ItemNotFoundException : DatabaseException
    {
        public ItemNotFoundException(string message) : base(message) { }
        public ItemNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }


    public class HandlerExistsException : DatabaseException
    {
        public HandlerExistsException(string message) : base(message) { }
        public HandlerExistsException(string message, Exception innerException) : base(message, innerException) { }
    }
}
