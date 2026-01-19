using System;
using AionLightning.Commons.Configuration.Transformers;

namespace AionLightning.Commons.Configuration
{
    [AttributeUsage(AttributeTargets.Field)]
    public class PropertyAttribute : Attribute
    {
        public const string DefaultValueString = "DO_NOT_OVERWRITE_INITIALIAZION_VALUE";

        public string Key { get; }
        public Type PropertyTransformer { get; }
        public string DefaultValue { get; }

        public PropertyAttribute(string key, Type propertyTransformer = null, string defaultValue = DefaultValueString)
        {
            Key = key;
            PropertyTransformer = propertyTransformer ?? typeof(IPropertyTransformer);
            DefaultValue = defaultValue;
        }
    }
}