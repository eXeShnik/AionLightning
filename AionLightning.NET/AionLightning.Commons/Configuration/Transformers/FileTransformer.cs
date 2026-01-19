using System.IO;
using System.Reflection;

namespace AionLightning.Commons.Configuration.Transformers
{
    public class FileTransformer : IPropertyTransformer
    {
        public static readonly FileTransformer SharedInstance = new();

        public object Transform(string value, FieldInfo field)
        {
            if (field.FieldType == typeof(FileInfo))
                return new FileInfo(value);
            if (field.FieldType == typeof(DirectoryInfo))
                return new DirectoryInfo(value);

            throw new TransformationException("Unsupported file type: " + field.FieldType.Name);
        }
    }
}