using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace AionLightning.Commons.Utils
{
    public static class ClassUtils
    {
        public static bool IsSubclass(Type a, Type b)
        {
            return a.IsSubclassOf(b) || a == b || (b.IsInterface && a.GetInterfaces().Contains(b));
        }

        public static bool IsPackageMember(Type clazz, string packageName)
        {
            return IsPackageMember(clazz.FullName, packageName);
        }

        public static bool IsPackageMember(string className, string packageName)
        {
            if (!className.Contains("."))
            {
                return string.IsNullOrEmpty(packageName);
            }
            var classPackage = className.Substring(0, className.LastIndexOf('.'));
            return classPackage == packageName;
        }

        public static IEnumerable<string> getClassNames(string jar)
        {
            return getClassNames(new FileInfo(jar));
        }

        public static IEnumerable<string> getClassNames(FileInfo file)
        {
            var classNames = new HashSet<string>();

            using (var archive = ZipFile.OpenRead(file.FullName))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.Name.EndsWith(".class"))
                    {
                        var className = entry.FullName.Replace('/', '.');
                        className = className.Substring(0, className.Length - ".class".Length);
                        classNames.Add(className);
                    }
                }
            }

            return classNames;
        }
    }
}
