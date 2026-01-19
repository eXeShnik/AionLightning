using System;

namespace AionLightning.Commons.Utils
{
    public static class Base64
    {
        public static string Encode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        public static byte[] Decode(string s)
        {
            return Convert.FromBase64String(s);
        }
    }
}
