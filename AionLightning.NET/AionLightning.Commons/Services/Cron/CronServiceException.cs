using System;

namespace AionLightning.Commons.Services.Cron
{
    public class CronServiceException : Exception
    {
        public CronServiceException()
        {
        }

        public CronServiceException(string message) : base(message)
        {
        }

        public CronServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
