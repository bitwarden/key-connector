using System;

namespace Bit.KeyConnector.Exceptions
{
    [Serializable]
    public class UnsupportedDatabaseProviderException(string message) : Exception(message);
}
