
using System;

namespace Authenta.SDK.Exceptions
{
    public class AuthentaException : Exception
    {
        public AuthentaException(string message) : base(message) { }
        public AuthentaException(string message, Exception inner) : base(message, inner) { }
    }
}
