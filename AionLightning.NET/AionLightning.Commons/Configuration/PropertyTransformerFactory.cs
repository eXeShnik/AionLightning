using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using AionLightning.Commons.Configuration.Transformers;
using AionLightning.Commons.Utils;

namespace AionLightning.Commons.Configuration
{
    public class PropertyTransformerFactory
    {
        public IPropertyTransformer GetTransformer(Type clazzToTransform)
        {
            if (clazzToTransform == typeof(bool))
                return BooleanTransformer.SharedInstance;
            if (clazzToTransform == typeof(byte))
                return ByteTransformer.SharedInstance;
            if (clazzToTransform == typeof(char))
                return CharTransformer.SharedInstance;
            if (clazzToTransform == typeof(double))
                return DoubleTransformer.SharedInstance;
            if (clazzToTransform == typeof(float))
                return FloatTransformer.SharedInstance;
            if (clazzToTransform == typeof(int))
                return IntegerTransformer.SharedInstance;
            if (clazzToTransform == typeof(long))
                return LongTransformer.SharedInstance;
            if (clazzToTransform == typeof(short))
                return ShortTransformer.SharedInstance;
            if (clazzToTransform == typeof(string))
                return StringTransformer.SharedInstance;
            if (clazzToTransform.IsEnum)
                return EnumTransformer.SharedInstance;
            if (clazzToTransform == typeof(FileInfo) || clazzToTransform == typeof(DirectoryInfo))
                return FileTransformer.SharedInstance;
            if (ClassUtils.IsSubclass(clazzToTransform, typeof(IPEndPoint)))
                return InetSocketAddressTransformer.SharedInstance;
            if (clazzToTransform == typeof(Regex))
                return PatternTransformer.SharedInstance;
            if (clazzToTransform == typeof(Type))
                return ClassTransformer.SharedInstance;

            throw new TransformationException("Transformer not found for class " + clazzToTransform.Name);
        }
    }
}