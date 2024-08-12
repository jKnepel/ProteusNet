using System;

namespace jKnepel.ProteusNet.Serializing
{
    public class SerializeNotImplemented : Exception
    {
        public SerializeNotImplemented() { }
        public SerializeNotImplemented(string message) : base(message) { }
        public SerializeNotImplemented(string message, Exception inner) : base(message, inner) { }
    }
}
