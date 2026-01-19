using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace AionLightning.Commons.Services
{
    public class ScriptManager
    {
        public void Load(FileInfo file)
        {
            throw new NotImplementedException();
        }

        public void Shutdown()
        {
            throw new NotImplementedException();
        }

    }

    public class ScriptService
    {
        private readonly ILogger<ScriptService> _log;
        private readonly Dictionary<string, ScriptManager> _map = new Dictionary<string, ScriptManager>();

        public ScriptService(ILogger<ScriptService> log)
        {
            _log = log;
        }

        public void Load(string file)
        {
            Load(new FileInfo(file));
        }

        public void Load(FileInfo file)
        {
            if (file.Exists)
            {
                if (file.Attributes.HasFlag(FileAttributes.Directory))
                {
                    LoadDir(new DirectoryInfo(file.FullName));
                }
                else
                {
                    LoadFile(file);
                }
            }
        }

        private void LoadFile(FileInfo file)
        {
            if (_map.ContainsKey(file.FullName))
                throw new ArgumentException($"ScriptManager by file: {file.FullName} already loaded");

            var sm = new ScriptManager();
            try
            {
                sm.Load(file);
            }
            catch (Exception e)
            {
                _log.LogError(e, "Failed to load script file");
                throw;
            }
            _map.Add(file.FullName, sm);
        }

        private void LoadDir(DirectoryInfo dir)
        {
            foreach (var file in dir.GetFiles("*.xml", SearchOption.AllDirectories))
            {
                LoadFile(file);
            }
        }

        public void Shutdown()
        {
            foreach (var sm in _map.Values)
            {
                sm.Shutdown();
            }
            _map.Clear();
        }
    }
}
