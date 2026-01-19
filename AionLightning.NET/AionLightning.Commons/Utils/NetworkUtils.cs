using System;
using System.Net;

namespace AionLightning.Commons.Utils
{
    public static class NetworkUtils
    {
        public static bool CheckIPMatching(string pattern, string address)
        {
            if (pattern == "*.*.*.*" || pattern == "*")
                return true;

            var mask = pattern.Split('.');
            var ipAddress = address.Split('.');
            for (var i = 0; i < mask.Length; i++)
            {
                if (mask[i] == "*" || mask[i] == ipAddress[i])
                    continue;
                if (mask[i].Contains("-"))
                {
                    var range = mask[i].Split('-');
                    if (byte.TryParse(range[0], out var min) &&
                        byte.TryParse(range[1], out var max) &&
                        byte.TryParse(ipAddress[i], out var ip))
                    {
                        if (ip < min || ip > max)
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
    }
}
