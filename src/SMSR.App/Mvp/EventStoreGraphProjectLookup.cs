using Microsoft.Data.Sqlite;
using System.IO;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<string?> FindGraphProjectByRootAsync(string root, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT project_id,root_path FROM graph_projects;";
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            if (string.Equals(Path.GetFullPath(reader.GetString(1)), Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))
                return reader.GetString(0);
        return null;
    }
}
