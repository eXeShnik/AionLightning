using System.Data.Common;

namespace AionLightning.Commons.Database
{
    public interface IParamReadStH : IReadStH
    {
        void SetParams(DbCommand stmt);
    }
}
