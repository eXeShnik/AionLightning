using System;
using System.Reflection;

namespace AionLightning.Commons.Configuration.Transformers
{
    public class ClassTransformer : IPropertyTransformer
    {
        public static readonly ClassTransformer SharedInstance = new();

        public object Transform(string value, FieldInfo field)
        {
            try
            {
                return Type.GetType(value, false);
            }
            catch (Exception e)
            {
                throw new TransformationException("Cannot find class with name '" + value + "'", e);
            }
        }
    }
}