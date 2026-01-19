using System;

namespace AionLightning.Commons.Database.DAO
{
    public class DAOAlreadyRegisteredException : DAOException
    {
        public DAOAlreadyRegisteredException()
        {
        }

        public DAOAlreadyRegisteredException(string message) : base(message)
        {
        }

        public DAOAlreadyRegisteredException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
