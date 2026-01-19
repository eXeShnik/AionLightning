using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace AionLightning.Commons.Database.DAO
{
    public class DAOManager
    {
        private readonly ILogger<DAOManager> _log;
        private readonly Dictionary<Type, DAO> _daoMap = new Dictionary<Type, DAO>();
        private readonly DAOLoader _loader;

        public DAOManager(ILogger<DAOManager> log)
        {
            _log = log;
            _loader = new DAOLoader(this);
        }

        public void RegisterDAO(DAO dao)
        {
            _daoMap[dao.GetDAOClass()] = dao;
        }

        public void UnregisterDAO(DAO dao)
        {
            _daoMap.Remove(dao.GetDAOClass());
        }

        public T GetDAO<T>() where T : DAO
        {
            return (T)_daoMap[typeof(T)];
        }

        public DAO GetDAO(Type type)
        {
            _daoMap.TryGetValue(type, out var dao);
            return dao;
        }

        public DAOLoader GetLoader()
        {
            return _loader;
        }

        public void Init()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    _loader.Load(assembly);
                }
                catch (Exception e)
                {
                    _log.LogError(e, "Failed to load daos from assembly");
                }
            }

            foreach (var dao in _daoMap)
            {
                _log.LogInformation($"Loaded DAO: {dao.Value.ClassName}");
            }
        }

        public void Shutdown()
        {
            _daoMap.Clear();
        }
    }
}
