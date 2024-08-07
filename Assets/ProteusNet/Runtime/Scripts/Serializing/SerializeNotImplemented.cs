using System;

namespace jKnepel.ProteusNet.Serialising
{
    public class SerializeNotImplemented : Exception
    {
        public SerializeNotImplemented() { }
        public SerializeNotImplemented(string message) : base(message) { }
        public SerializeNotImplemented(string message, Exception inner) : base(message, inner) { }
    }
}
