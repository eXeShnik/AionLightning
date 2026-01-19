using System;

namespace AionLightning.Commons.Configuration
{
    public class TransformationException : Exception
    {
        public TransformationException()
        {
        }

        public TransformationException(string message) : base(message)
        {
        }

        public TransformationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}