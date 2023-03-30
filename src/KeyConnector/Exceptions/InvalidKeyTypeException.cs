using System;

namespace Bit.KeyConnector.Exceptions
{
    public class InvalidKeyTypeException : Exception
    {
        public InvalidKeyTypeException()
            : base("This type of key cannot perform this action.") { }

        public InvalidKeyTypeException(string message) : base(message) { }
    }
}
