using System.Globalization;
using System.Reflection;

namespace AionLightning.Commons.Configuration.Transformers
{
    public class ShortTransformer : IPropertyTransformer
    {
        public static readonly ShortTransformer SharedInstance = new();

        public object Transform(string value, FieldInfo field)
        {
            try
            {
                if (value.StartsWith("0x", System.StringComparison.OrdinalIgnoreCase))
                {
                    return short.Parse(value.Substring(2), NumberStyles.HexNumber);
                }

                return short.Parse(value);
            }
            catch (System.Exception e)
            {
                throw new TransformationException(e.Message, e);
            }
        }
    }
}