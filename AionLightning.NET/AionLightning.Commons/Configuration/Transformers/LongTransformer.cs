using System.Globalization;
using System.Reflection;

namespace AionLightning.Commons.Configuration.Transformers
{
    public class LongTransformer : IPropertyTransformer
    {
        public static readonly LongTransformer SharedInstance = new();

        public object Transform(string value, FieldInfo field)
        {
            try
            {
                if (value.StartsWith("0x", System.StringComparison.OrdinalIgnoreCase))
                {
                    return long.Parse(value.Substring(2), NumberStyles.HexNumber);
                }

                return long.Parse(value);
            }
            catch (System.Exception e)
            {
                throw new TransformationException(e.Message, e);
            }
        }
    }
}