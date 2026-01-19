using System.Data.Common;

namespace AionLightning.Commons.Database
{
    public interface ICallReadStH : IReadStH
    {
        void SetParams(DbCommand stmt);
    }
}
