using System.Net;
using System.Reflection;

namespace AionLightning.Commons.Configuration.Transformers
{
    public class InetSocketAddressTransformer : IPropertyTransformer
    {
        public static readonly InetSocketAddressTransformer SharedInstance = new();

        public object Transform(string value, FieldInfo field)
        {
            var parts = value.Split(':');

            if (parts.Length != 2)
            {
                throw new TransformationException("Can't transform property, must be in format \"address:port\"");
            }

            try
            {
                var port = int.Parse(parts[1]);
                if ("*".Equals(parts[0]))
                {
                    return new IPEndPoint(IPAddress.Any, port);
                }

                var address = IPAddress.Parse(parts[0]);
                return new IPEndPoint(address, port);
            }
            catch (System.Exception e)
            {
                throw new TransformationException(e.Message, e);
            }
        }
    }
}