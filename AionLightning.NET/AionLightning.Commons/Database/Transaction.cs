
using System;
using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Logging;

namespace AionLightning.Commons.Database
{
    public class Transaction : IDisposable
    {
        private readonly ILogger<Transaction> _log;

        private DbConnection _connection;
        private DbTransaction _transaction;

        internal Transaction(DbConnection con, ILogger<Transaction> log)
        {
            _log = log;
            _connection = con;
            _transaction = _connection.BeginTransaction();
        }

        public DbConnection Connection => _connection;

        public void InsertUpdate(string sql)
        {
            InsertUpdate(sql, null);
        }

        public void InsertUpdate(string sql, IIUStH iusth)
        {
            using (var statement = _connection.CreateCommand())
            {
                statement.Transaction = _transaction;
                statement.CommandText = sql;
                if (iusth != null)
                {
                    iusth.HandleInsertUpdate(statement);
                }
                else
                {
                    statement.ExecuteNonQuery();
                }
            }
        }

        public void Commit()
        {
            try
            {
                _transaction.Commit();
            }
            catch (Exception e)
            {
                _log.LogError(e, "Failed to commit transaction");
            }
        }

        public void Rollback()
        {
            try
            {
                _transaction.Rollback();
            }
            catch (Exception e)
            {
                _log.LogError(e, "Failed to rollback transaction");
            }
        }

        public void Dispose()
        {
            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }
    }
}
