using System.Reflection;

namespace AionLightning.Commons.Configuration
{
    public interface IPropertyTransformer
    {
        object Transform(string value, FieldInfo field);
    }
}