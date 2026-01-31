using System.Runtime.Serialization;

namespace AionServer.Commons.Database;

public class DaoAlreadyRegisteredException : DaoException
{
    public DaoAlreadyRegisteredException()
    {
    }
    
    protected DaoAlreadyRegisteredException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
    
    public DaoAlreadyRegisteredException(string? message) : base(message)
    {
    }
    
    public DaoAlreadyRegisteredException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}