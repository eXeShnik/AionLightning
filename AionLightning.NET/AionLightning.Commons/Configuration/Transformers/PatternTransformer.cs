using System.Reflection;
using System.Text.RegularExpressions;

namespace AionLightning.Commons.Configuration.Transformers
{
    public class PatternTransformer : IPropertyTransformer
    {
        public static readonly PatternTransformer SharedInstance = new();

        public object Transform(string value, FieldInfo field)
        {
            try
            {
                return new Regex(value);
            }
            catch (System.Exception e)
            {
                throw new TransformationException("Not valid RegExp: " + value, e);
            }
        }
    }
}