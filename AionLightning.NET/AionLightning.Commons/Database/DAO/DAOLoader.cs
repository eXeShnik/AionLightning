using AionLightning.Commons.Utils;
using System;
using System.Linq;
using System.Reflection;

namespace AionLightning.Commons.Database.DAO
{
    public class DAOLoader
    {
        private readonly DAOManager _daoManager;

        public DAOLoader(DAOManager daoManager)
        {
            _daoManager = daoManager;
        }

        public void Load(DAO dao)
        {
            _daoManager.RegisterDAO(dao);
        }

        public void Unload(DAO dao)
        {
            _daoManager.UnregisterDAO(dao);
        }

        public DAO GetLoadedDAO(Type daoClass)
        {
            return _daoManager.GetDAO(daoClass);
        }

        public void Load(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (IsValidDAO(type))
                {
                    try
                    {
                        var dao = (DAO)Activator.CreateInstance(type);
                        _daoManager.RegisterDAO(dao);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Can't register DAO class", e);
                    }
                }
            }
        }

        public void Unload(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (IsValidDAO(type))
                {
                    try
                    {
                        var dao = _daoManager.GetDAO(type);
                        if (dao != null)
                            _daoManager.UnregisterDAO(dao);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Can't unregister DAO class", e);
                    }
                }
            }
        }

        public bool IsValidDAO(Type clazz)
        {
            if (!ClassUtils.IsSubclass(clazz, typeof(IDAO)))
                return false;

            if (clazz.IsAbstract || clazz.IsInterface)
                return false;

            if (!clazz.IsPublic)
                return false;

            if (clazz.GetCustomAttributes(typeof(DisabledDAOAttribute), false).Any())
                return false;

            return true;
        }
    }
}
