using System;

namespace Nova.Scripts
{
    public class ConfigurationException : DatabaseException
    {
        public ConfigurationException(string message) : base(message) { }
        public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ConfigurationAlreadyExists : ConfigurationException
    {
        public ConfigurationAlreadyExists(string message) : base(message) { }
        public ConfigurationAlreadyExists(string message, Exception innerException) : base(message, innerException) { }
    }

}
