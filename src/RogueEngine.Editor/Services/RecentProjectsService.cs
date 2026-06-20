using System.Text.Json;
using RogueEngine.Editor.Models;

namespace RogueEngine.Editor.Services;

public sealed class RecentProjectsService
{
    private const int MaxEntries = 12;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _storePath;

    public RecentProjectsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "RogueEngine");
        Directory.CreateDirectory(folder);
        _storePath = Path.Combine(folder, "recent-projects.json");
    }

    public IReadOnlyList<RecentProjectEntry> Load()
    {
        if (!File.Exists(_storePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_storePath);
            var entries = JsonSerializer.Deserialize<List<RecentProjectEntry>>(json, JsonOptions) ?? [];
            return entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.ReprojPath))
                .Where(entry => File.Exists(entry.ReprojPath))
                .OrderByDescending(entry => entry.LastOpenedUtc)
                .Take(MaxEntries)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public void RecordOpened(string reprojPath, string projectName)
    {
        var entries = Load().ToList();
        entries.RemoveAll(entry => string.Equals(entry.ReprojPath, reprojPath, StringComparison.OrdinalIgnoreCase));
        entries.Insert(0, new RecentProjectEntry
        {
            Name = projectName,
            ReprojPath = Path.GetFullPath(reprojPath),
            LastOpenedUtc = DateTime.UtcNow
        });

        if (entries.Count > MaxEntries)
        {
            entries = entries.Take(MaxEntries).ToList();
        }

        var json = JsonSerializer.Serialize(entries, JsonOptions);
        File.WriteAllText(_storePath, json);
    }
}
