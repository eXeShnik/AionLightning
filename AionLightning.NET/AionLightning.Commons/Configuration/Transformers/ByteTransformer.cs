using System.Globalization;
using System.Reflection;

namespace AionLightning.Commons.Configuration.Transformers
{
    public class ByteTransformer : IPropertyTransformer
    {
        public static readonly ByteTransformer SharedInstance = new();

        public object Transform(string value, FieldInfo field)
        {
            try
            {
                if (value.StartsWith("0x", System.StringComparison.OrdinalIgnoreCase))
                {
                    return byte.Parse(value.Substring(2), NumberStyles.HexNumber);
                }

                return byte.Parse(value);
            }
            catch (System.Exception e)
            {
                throw new TransformationException(e.Message, e);
            }
        }
    }
}