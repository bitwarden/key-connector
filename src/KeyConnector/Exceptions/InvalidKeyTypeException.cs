using System;
using System.Runtime.Serialization;

namespace Bit.KeyConnector.Exceptions
{
    [Serializable]
    public class InvalidKeyTypeException : Exception
    {
        public InvalidKeyTypeException()
            : base("This type of key cannot perform this action.") { }

        public InvalidKeyTypeException(string message) : base(message) { }

        public InvalidKeyTypeException(string message, Exception innerException)
            : base(message, innerException) { }

        protected InvalidKeyTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
