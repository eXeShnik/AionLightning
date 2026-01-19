using System.Collections;
using System.Collections.Generic;

namespace AionLightning.Commons.Utils
{
    public static class GenericValidator
    {
        public static bool IsBlankOrNull(string s)
        {
            return string.IsNullOrEmpty(s);
        }

        public static bool IsBlankOrNull(ICollection c)
        {
            return c == null || c.Count == 0;
        }

        public static bool IsBlankOrNull(IDictionary m)
        {
            return m == null || m.Count == 0;
        }

        public static bool IsBlankOrNull(object[] a)
        {
            return a == null || a.Length == 0;
        }
    }
}
