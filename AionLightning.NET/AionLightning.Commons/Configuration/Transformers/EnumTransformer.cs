using System;
using System.Reflection;

namespace AionLightning.Commons.Configuration.Transformers
{
    public class EnumTransformer : IPropertyTransformer
    {
        public static readonly EnumTransformer SharedInstance = new();

        public object Transform(string value, FieldInfo field)
        {
            try
            {
                return Enum.Parse(field.FieldType, value, true);
            }
            catch (Exception e)
            {
                throw new TransformationException(e.Message, e);
            }
        }
    }
}