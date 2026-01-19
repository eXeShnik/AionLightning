using System;

namespace AionLightning.Commons.Database.DAO
{
    public class DAONotFoundException : DAOException
    {
        public DAONotFoundException()
        {
        }

        public DAONotFoundException(string message) : base(message)
        {
        }

        public DAONotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
