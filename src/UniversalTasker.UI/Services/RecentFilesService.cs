using System.IO;
using System.Text.Json;

namespace UniversalTasker.UI.Services;

public class RecentFilesService
{
    public const int MaxRecentFiles = 10;

    private static readonly string FilePath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "UniversalTasker", "recentfiles.json");

    private List<string> _recentFiles = new();

    public RecentFilesService()
    {
        Load();
    }

    public IReadOnlyList<string> GetRecentFiles() => _recentFiles.AsReadOnly();

    public void AddFile(string path)
    {
        _recentFiles.Remove(path);
        _recentFiles.Insert(0, path);
        if (_recentFiles.Count > MaxRecentFiles)
            _recentFiles.RemoveRange(MaxRecentFiles, _recentFiles.Count - MaxRecentFiles);
        Save();
    }

    public void RemoveFile(string path)
    {
        if (_recentFiles.Remove(path))
            Save();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var list = JsonSerializer.Deserialize<List<string>>(json) ?? new();
                // Only keep entries that still exist on disk
                _recentFiles = list.Where(File.Exists).ToList();
            }
        }
        catch
        {
            _recentFiles = new();
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(_recentFiles));
        }
        catch
        {
            // Non-critical — ignore save failures
        }
    }
}
