using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace MESv2.Migration.Readers;

/// <summary>
/// Base reader for v1 QSCApps database tables. Uses Dapper for raw SQL reads.
/// All queries return dynamic objects to avoid maintaining a parallel v1 model.
/// </summary>
public class V1Reader : IDisposable
{
    private readonly SqlConnection _conn;

    public V1Reader(string connectionString)
    {
        _conn = new SqlConnection(connectionString);
    }

    public async Task OpenAsync() => await _conn.OpenAsync();

    public async Task<IEnumerable<dynamic>> ReadTableAsync(string tableName, string? whereClause = null)
    {
        var sql = $"SELECT * FROM [dbo].[{tableName}]";
        if (!string.IsNullOrEmpty(whereClause))
            sql += $" WHERE {whereClause}";
        return await _conn.QueryAsync(sql);
    }

    public async Task<int> CountAsync(string tableName, string? whereClause = null)
    {
        var sql = $"SELECT COUNT(*) FROM [dbo].[{tableName}]";
        if (!string.IsNullOrEmpty(whereClause))
            sql += $" WHERE {whereClause}";
        return await _conn.ExecuteScalarAsync<int>(sql);
    }

    public async Task<IEnumerable<dynamic>> QueryAsync(string sql, object? param = null)
    {
        return await _conn.QueryAsync(sql, param);
    }

    public async Task<T?> ScalarAsync<T>(string sql, object? param = null)
    {
        return await _conn.ExecuteScalarAsync<T>(sql, param);
    }

    public void Dispose() => _conn.Dispose();
}
