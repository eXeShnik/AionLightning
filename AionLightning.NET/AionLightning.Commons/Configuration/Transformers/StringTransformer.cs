using System.Reflection;

namespace AionLightning.Commons.Configuration.Transformers
{
    public class StringTransformer : IPropertyTransformer
    {
        public static readonly StringTransformer SharedInstance = new StringTransformer();

        public object Transform(string value, FieldInfo field)
        {
            return value;
        }
    }
}