using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Combination.Observables.Channels
{
    [Serializable]
    public sealed class BufferOverflowException : Exception
    {
        public BufferOverflowException(string message) : base(message)
        {
        }
        private BufferOverflowException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
