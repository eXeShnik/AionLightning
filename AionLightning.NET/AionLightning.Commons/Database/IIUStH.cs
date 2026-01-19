using System.Data.Common;

namespace AionLightning.Commons.Database
{
    public interface IIUStH
    {
        void HandleInsertUpdate(DbCommand stmt);
    }
}
