using System.Data.Common;

namespace AionLightning.Commons.Database
{
    public interface IReadStH
    {
        void HandleRead(DbDataReader rset);
    }
}
