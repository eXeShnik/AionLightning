using System.Reflection;

namespace AionLightning.Commons.Configuration.Transformers
{
    public class FloatTransformer : IPropertyTransformer
    {
        public static readonly FloatTransformer SharedInstance = new();

        public object Transform(string value, FieldInfo field)
        {
            try
            {
                return float.Parse(value);
            }
            catch (System.Exception e)
            {
                throw new TransformationException(e.Message, e);
            }
        }
    }
}