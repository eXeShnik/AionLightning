namespace AionServer.Commons.Database;

public class DaoAlreadyRegisteredException : DaoException
{
    public DaoAlreadyRegisteredException()
    {
    }
    
    public DaoAlreadyRegisteredException(string? message) : base(message)
    {
    }
    
    public DaoAlreadyRegisteredException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}