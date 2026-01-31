using System.Runtime.Serialization;

namespace AionServer.Commons.Database;

public class DaoException : Exception
{
    public DaoException()
    {
    }
    
    protected DaoException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
    
    public DaoException(string? message) : base(message)
    {
    }
    
    public DaoException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}