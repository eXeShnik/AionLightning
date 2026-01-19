using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Logging;

namespace AionLightning.Commons.Database
{
    public sealed class DB
    {
        private readonly ILogger<DB> _log;
        private readonly DatabaseFactory _databaseFactory;

        public DB(ILogger<DB> log, DatabaseFactory databaseFactory)
        {
            _log = log;
            _databaseFactory = databaseFactory;
        }

        public bool Select(string query, IReadStH reader)
        {
            return Select(query, reader, null);
        }

        public bool Select(string query, IReadStH reader, DbConnection con)
        {
            var start = DateTime.Now;
            var connection = con;
            DbCommand st = null;
            DbDataReader rs = null;

            try
            {
                if (connection == null)
                    connection = _databaseFactory.GetConnection();

                st = _databaseFactory.CreateStatement(connection);
                rs = st.ExecuteReader();
                reader.HandleRead(rs);
            }
            catch (Exception e)
            {
                _log.LogError(e, "Could not execute query " + query);
                return false;
            }
            finally
            {
                Close(rs);
                Close(st);
                if (con == null)
                    Close(connection);
            }

            var time = DateTime.Now - start;
            if (time.TotalMilliseconds > 100)
                _log.LogWarning("Query slow " + time.TotalMilliseconds + "ms: " + query);

            return true;
        }

        public bool Update(string query)
        {
            return Update(query, null);
        }

        public bool Update(string query, DbConnection con)
        {
            var start = DateTime.Now;
            var connection = con;
            DbCommand st = null;

            try
            {
                if (connection == null)
                    connection = _databaseFactory.GetConnection();

                st = _databaseFactory.CreateStatement(connection);
                st.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                _log.LogError(e, "Could not execute update " + query);
                return false;
            }
            finally
            {
                Close(st);
                if (con == null)
                    Close(connection);
            }

            var time = DateTime.Now - start;
            if (time.TotalMilliseconds > 100)
                _log.LogWarning("Query slow " + time.TotalMilliseconds + "ms: " + query);

            return true;
        }

        public bool InsertUpdate(string query, IIUStH handler)
        {
            return InsertUpdate(query, handler, null);
        }

        public bool InsertUpdate(string query, IIUStH handler, DbConnection con)
        {
            var start = DateTime.Now;
            var connection = con;
            DbCommand st = null;
            DbDataReader rs = null;

            try
            {
                if (connection == null)
                    connection = _databaseFactory.GetConnection();

                st = _databaseFactory.CreateStatement(connection);
                st.CommandText = query;
                st.ExecuteNonQuery();
                handler.HandleInsertUpdate(st);
            }
            catch (Exception e)
            {
                _log.LogError(e, "Could not execute insert/update " + query);
                return false;
            }
            finally
            {
                Close(st);
                if (con == null)
                    Close(connection);
            }

            var time = DateTime.Now - start;
            if (time.TotalMilliseconds > 100)
                _log.LogWarning("Query slow " + time.TotalMilliseconds + "ms: " + query);

            return true;
        }

        public void Close(DbConnection con)
        {
            try
            {
                con.Close();
            }
            catch (Exception e)
            {
                _log.LogError(e, "Could not close connection");
            }
        }

        public void Close(DbCommand st)
        {
            try
            {
                st.Dispose();
            }
            catch (Exception e)
            {
                _log.LogError(e, "Could not close statement");
            }
        }

        public void Close(DbDataReader rs)
        {
            if (rs == null) return;
            try
            {
                rs.Close();
            }
            catch (Exception e)
            {
                _log.LogError(e, "Could not close result set");
            }
        }
    }
}
