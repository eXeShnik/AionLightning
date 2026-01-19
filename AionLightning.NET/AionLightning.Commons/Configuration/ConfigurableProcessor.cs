using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using AionLightning.Commons.Configuration.Transformers;

namespace AionLightning.Commons.Configuration
{
    public class ConfigurableProcessor
    {
        private readonly ILogger<ConfigurableProcessor> _log;
        private readonly PropertyTransformerFactory _propertyTransformerFactory;

        public ConfigurableProcessor(ILogger<ConfigurableProcessor> log, PropertyTransformerFactory propertyTransformerFactory)
        {
            _log = log;
            _propertyTransformerFactory = propertyTransformerFactory;
        }

        public void Process(object target, IConfiguration configuration)
        {
            var type = target as Type ?? target.GetType();
            var isStatic = target is Type;

            Process(type, isStatic ? null : target, configuration);
        }

        private void Process(Type type, object obj, IConfiguration configuration)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                               (obj == null ? BindingFlags.Static : BindingFlags.Instance)))
            {
                var propertyAttribute = (PropertyAttribute)Attribute.GetCustomAttribute(field, typeof(PropertyAttribute));
                if (propertyAttribute == null)
                    continue;

                if (field.IsLiteral && !field.IsInitOnly)
                {
                    _log.LogError($"Attempt to process final field {field.Name} of class {type.Name}");
                    throw new InvalidOperationException($"Field {field.Name} of class {type.Name} is final.");
                }

                try
                {
                    var value = GetValue(field, configuration);
                    if (value != null)
                    {
                        SetValue(field, obj, value);
                    }
                }
                catch (Exception e)
                {
                    _log.LogError(e, $"Error during processing field: {field.Name} of class {type.Name}");
                }
            }
        }

        private void SetValue(FieldInfo field, object obj, string value)
        {
            var propertyAttribute = (PropertyAttribute)Attribute.GetCustomAttribute(field, typeof(PropertyAttribute));
            if (propertyAttribute != null)
            {
                var transformer = _propertyTransformerFactory.GetTransformer(field.FieldType);
                var transformedValue = transformer.Transform(value, field);
                field.SetValue(obj, transformedValue);
            }
        }

        private string GetValue(FieldInfo field, IConfiguration configuration)
        {
            var propertyAttribute = (PropertyAttribute)Attribute.GetCustomAttribute(field, typeof(PropertyAttribute));
            var value = configuration[propertyAttribute.Key];

            if (string.IsNullOrEmpty(value))
            {
                if (propertyAttribute.DefaultValue != PropertyAttribute.DefaultValueString)
                {
                    value = propertyAttribute.DefaultValue;
                    _log.LogDebug($"Using default value for field {field.Name} of class {field.DeclaringType.Name}");
                }
                else
                {
                    _log.LogDebug($"Field {field.Name} of class {field.DeclaringType.Name} wasn't modified");
                    return null;
                }
            }

            return value;
        }
    }
}