using System.Globalization;
using System.Reflection;

namespace AionLightning.Commons.Configuration.Transformers
{
    public class IntegerTransformer : IPropertyTransformer
    {
        public static readonly IntegerTransformer SharedInstance = new();

        public object Transform(string value, FieldInfo field)
        {
            try
            {
                if (value.StartsWith("0x", System.StringComparison.OrdinalIgnoreCase))
                {
                    return int.Parse(value.Substring(2), NumberStyles.HexNumber);
                }
                return int.Parse(value);
            }
            catch (System.Exception e)
            {
                throw new TransformationException(e.Message, e);
            }
        }
    }
}