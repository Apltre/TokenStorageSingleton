using System;

namespace TokenStorageSingleton.Exceptions
{
    public class RepeatableRequestException : Exception
    {

        public RepeatableRequestException(string message) : base(message)
        {
        }
    }
}
