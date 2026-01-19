using System.Reflection;

namespace AionLightning.Commons.Configuration.Transformers
{
    public class DoubleTransformer : IPropertyTransformer
    {
        public static readonly DoubleTransformer SharedInstance = new();

        public object Transform(string value, FieldInfo field)
        {
            try
            {
                return double.Parse(value);
            }
            catch (System.Exception e)
            {
                throw new TransformationException(e.Message, e);
            }
        }
    }
}