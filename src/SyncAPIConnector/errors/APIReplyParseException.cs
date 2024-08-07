using System;
using System.Runtime.Serialization;

namespace xAPI.Errors;

[Serializable]
public class APIReplyParseException : Exception
{
    public APIReplyParseException()
        : base()
    {
    }

    public APIReplyParseException(string message)
        : base(message)
    {
    }

    public APIReplyParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected APIReplyParseException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}