using System.Reflection;

namespace AionLightning.Commons.Configuration.Transformers
{
    public class CharTransformer : IPropertyTransformer
    {
        public static readonly CharTransformer SharedInstance = new();

        public object Transform(string value, FieldInfo field)
        {
            try
            {
                var chars = value.ToCharArray();
                if (chars.Length > 1)
                {
                    throw new TransformationException("To many characters in the value");
                }

                return chars[0];
            }
            catch (System.Exception e)
            {
                throw new TransformationException(e.Message, e);
            }
        }
    }
}