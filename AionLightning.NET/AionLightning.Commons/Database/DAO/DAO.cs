using System;

namespace AionLightning.Commons.Database.DAO
{
    public abstract class DAO
    {
        public abstract Type GetDAOClass();
        public string ClassName => GetType().Name;
    }
}
