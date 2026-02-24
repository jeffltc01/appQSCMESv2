using System.Text.Json;

namespace MESv2.Migration;

public class TableMigrationResult
{
    public string TableName { get; set; } = string.Empty;
    public int SourceCount { get; set; }
    public int MigratedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Warnings { get; set; } = new();
    public int WarningCount { get; set; }
    public int WarningStorageSuppressedCount { get; set; }
    public TimeSpan Duration { get; set; }
}

public class MigrationLogger
{
    private const int MaxWarningsPrintedPerTable = 50;
    private const int MaxWarningsStoredPerTable = 250;

    private readonly List<TableMigrationResult> _results = new();
    private TableMigrationResult? _current;
    private int _printedWarningsThisTable;

    public void BeginTable(string tableName)
    {
        _current = new TableMigrationResult { TableName = tableName };
        _printedWarningsThisTable = 0;
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
        if (_current == null)
            return;

        _current.WarningCount++;

        if (_current.Warnings.Count < MaxWarningsStoredPerTable)
            _current.Warnings.Add(message);
        else
            _current.WarningStorageSuppressedCount++;

        if (_printedWarningsThisTable < MaxWarningsPrintedPerTable)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  WARN: {message}");
            Console.ResetColor();
            _printedWarningsThisTable++;

            if (_printedWarningsThisTable == MaxWarningsPrintedPerTable)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  WARN: Further warnings for this table are suppressed in console output (limit: {MaxWarningsPrintedPerTable}).");
                Console.ResetColor();
            }
        }
    }

    public void EndTable(TimeSpan duration)
    {
        if (_current == null) return;
        _current.Duration = duration;
        Console.WriteLine($"  Migrated: {_current.MigratedCount}, Skipped: {_current.SkippedCount}, Warnings: {_current.WarningCount}, Time: {duration.TotalSeconds:F1}s");
        if (_current.WarningStorageSuppressedCount > 0)
            Console.WriteLine($"  Note: {_current.WarningStorageSuppressedCount} warnings were omitted from the JSON report to keep file size manageable.");
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
            Console.WriteLine($"{r.TableName,-35} {r.SourceCount,8} {r.MigratedCount,8} {r.SkippedCount,8} {r.WarningCount,6}");
        }
        Console.WriteLine(new string('-', 70));
        Console.WriteLine($"{"TOTAL",-35} {_results.Sum(r => r.SourceCount),8} {_results.Sum(r => r.MigratedCount),8} {_results.Sum(r => r.SkippedCount),8} {_results.Sum(r => r.WarningCount),6}");
    }

    public void SaveReport(string path)
    {
        var json = JsonSerializer.Serialize(_results, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
        Console.WriteLine($"\nReport saved to: {path}");
    }

    public IReadOnlyList<TableMigrationResult> Results => _results.AsReadOnly();
}
