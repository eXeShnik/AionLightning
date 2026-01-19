using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace AionLightning.Commons.Utils
{
    public static class AEInfos
    {
        public static string[] GetMemoryInfo()
        {
            var process = Process.GetCurrentProcess();
            var allocated = process.WorkingSet64 / 1024.0;
            var max = process.MaxWorkingSet.ToInt64() / 1024.0;
            var used = process.PrivateMemorySize64 / 1024.0;

            var df = " (0.0000'%)'";
            var df2 = " # 'KB'";

            return new[]
            {
                "+----",
                "| Global Memory Informations at " + DateTime.Now,
                "|    |",
                "| Allowed Memory:" + max.ToString(df2, CultureInfo.InvariantCulture),
                "|    |= Allocated Memory:" + allocated.ToString(df2, CultureInfo.InvariantCulture) + (allocated / max).ToString(df, CultureInfo.InvariantCulture),
                "|    |= Non-Allocated Memory:" + (max - allocated).ToString(df2, CultureInfo.InvariantCulture) + ((max - allocated) / max).ToString(df, CultureInfo.InvariantCulture),
                "| Allocated Memory:" + allocated.ToString(df2, CultureInfo.InvariantCulture),
                "|    |= Used Memory:" + used.ToString(df2, CultureInfo.InvariantCulture) + (used / max).ToString(df, CultureInfo.InvariantCulture),
                "|    |= Unused (cached) Memory:" + (allocated - used).ToString(df2, CultureInfo.InvariantCulture) + ((allocated - used) / max).ToString(df, CultureInfo.InvariantCulture),
                "| Useable Memory:" + (max - used).ToString(df2, CultureInfo.InvariantCulture) + ((max - used) / max).ToString(df, CultureInfo.InvariantCulture),
                "+----"
            };
        }

        public static string[] GetCPUInfo()
        {
            return new[]
            {
                "Available CPU(s): " + Environment.ProcessorCount,
                "Processor(s) Identifier: " + Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
                "..................................................",
                ".................................................."
            };
        }

        public static string[] GetOSInfo()
        {
            return new[]
            {
                "OS: " + RuntimeInformation.OSDescription + " Build: " + Environment.OSVersion.Version,
                "OS Arch: " + RuntimeInformation.OSArchitecture,
                "..................................................",
                ".................................................."
            };
        }

        public static string[] GetJREInfo()
        {
            return new[]
            {
                ".NET Platform Information",
                ".NET Runtime  Name: " + RuntimeInformation.FrameworkDescription,
                ".NET Version: " + Environment.Version,
                "..................................................",
                ".................................................."
            };
        }

        public static string[] GetJVMInfo()
        {
            return new[]
            {
                "Virtual Machine Information (CLR)",
                "CLR Name: " + Type.GetType("Mono.Runtime") != null ? "Mono" : ".NET",
                "CLR installation directory: " + RuntimeEnvironment.GetRuntimeDirectory(),
                "CLR version: " + Environment.Version,
                "..................................................",
                ".................................................."
            };
        }

        public static void PrintAllInfos()
        {
            foreach (var line in GetMemoryInfo())
                Console.WriteLine(line);
            foreach (var line in GetCPUInfo())
                Console.WriteLine(line);
            foreach (var line in GetOSInfo())
                Console.WriteLine(line);
            foreach (var line in GetJREInfo())
                Console.WriteLine(line);
            foreach (var line in GetJVMInfo())
                Console.WriteLine(line);
        }
    }
}
