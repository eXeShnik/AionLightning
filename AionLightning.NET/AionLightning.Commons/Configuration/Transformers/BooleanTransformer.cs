using System.Reflection;

namespace AionLightning.Commons.Configuration.Transformers
{
    public class BooleanTransformer : IPropertyTransformer
    {
        public static readonly BooleanTransformer SharedInstance = new();

        public object Transform(string value, FieldInfo field)
        {
            if ("true".Equals(value, System.StringComparison.OrdinalIgnoreCase) || "1".Equals(value))
            {
                return true;
            }
            if ("false".Equals(value, System.StringComparison.OrdinalIgnoreCase) || "0".Equals(value))
            {
                return false;
            }
            throw new TransformationException("Invalid boolean string: " + value);
        }
    }
}