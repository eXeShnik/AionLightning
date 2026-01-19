using System;
using System.Text.RegularExpressions;

namespace AionLightning.Commons.Utils
{
    public static class SystemPropertyUtil
    {
        public static bool Contains(string key)
        {
            return Get(key) != null;
        }

        public static string Get(string key)
        {
            return Get(key, null);
        }

        public static string Get(string key, string def)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("key must not be empty.", nameof(key));
            }

            try
            {
                var value = Environment.GetEnvironmentVariable(key);
                return value ?? def;
            }
            catch (Exception)
            {
                // Log exception
                return def;
            }
        }

        public static bool GetBoolean(string key, bool def)
        {
            var value = Get(key);
            if (value == null)
            {
                return def;
            }

            value = value.Trim().ToLower();
            if (value.Length == 0)
            {
                return true;
            }

            if ("true".Equals(value) || "yes".Equals(value) || "1".Equals(value))
            {
                return true;
            }

            if ("false".Equals(value) || "no".Equals(value) || "0".Equals(value))
            {
                return false;
            }

            // Log warning
            return def;
        }

        public static int GetInt(string key, int def)
        {
            var value = Get(key);
            if (value == null)
            {
                return def;
            }

            value = value.Trim().ToLower();
            if (int.TryParse(value, out var result))
            {
                return result;
            }

            return def;
        }

        public static long GetLong(string key, long def)
        {
            var value = Get(key);
            if (value == null)
            {
                return def;
            }

            value = value.Trim().ToLower();
            if (long.TryParse(value, out var result))
            {
                return result;
            }

            return def;
        }
    }
}
