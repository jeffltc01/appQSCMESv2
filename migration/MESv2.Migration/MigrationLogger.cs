using System.Text.Json;

namespace MESv2.Migration;

public class TableMigrationResult
{
    public string TableName { get; set; } = string.Empty;
    public int SourceCount { get; set; }
    public int MigratedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Warnings { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

public class MigrationLogger
{
    private readonly List<TableMigrationResult> _results = new();
    private TableMigrationResult? _current;

    public void BeginTable(string tableName)
    {
        _current = new TableMigrationResult { TableName = tableName };
        Console.WriteLine();
        Console.WriteLine($"=== Migrating: {tableName} ===");
    }

    public void SetSourceCount(int count)
    {
        if (_current != null) _current.SourceCount = count;
        Console.WriteLine($"  Source rows: {count}");
    }

    public void SetMigratedCount(int count)
    {
        if (_current != null) _current.MigratedCount = count;
    }

    public void IncrementMigrated(int count = 1)
    {
        if (_current != null) _current.MigratedCount += count;
    }

    public void IncrementSkipped(int count = 1)
    {
        if (_current != null) _current.SkippedCount += count;
    }

    public void Warn(string message)
    {
        _current?.Warnings.Add(message);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  WARN: {message}");
        Console.ResetColor();
    }

    public void EndTable(TimeSpan duration)
    {
        if (_current == null) return;
        _current.Duration = duration;
        Console.WriteLine($"  Migrated: {_current.MigratedCount}, Skipped: {_current.SkippedCount}, Warnings: {_current.Warnings.Count}, Time: {duration.TotalSeconds:F1}s");
        _results.Add(_current);
        _current = null;
    }

    public void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("       MIGRATION SUMMARY");
        Console.WriteLine("========================================");
        Console.WriteLine($"{"Table",-35} {"Source",8} {"Migrated",8} {"Skipped",8} {"Warns",6}");
        Console.WriteLine(new string('-', 70));
        foreach (var r in _results)
        {
            Console.WriteLine($"{r.TableName,-35} {r.SourceCount,8} {r.MigratedCount,8} {r.SkippedCount,8} {r.Warnings.Count,6}");
        }
        Console.WriteLine(new string('-', 70));
        Console.WriteLine($"{"TOTAL",-35} {_results.Sum(r => r.SourceCount),8} {_results.Sum(r => r.MigratedCount),8} {_results.Sum(r => r.SkippedCount),8} {_results.Sum(r => r.Warnings.Count),6}");
    }

    public void SaveReport(string path)
    {
        var json = JsonSerializer.Serialize(_results, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
        Console.WriteLine($"\nReport saved to: {path}");
    }

    public IReadOnlyList<TableMigrationResult> Results => _results.AsReadOnly();
}
