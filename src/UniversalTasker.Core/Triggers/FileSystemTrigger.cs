using Microsoft.Extensions.Logging;

namespace UniversalTasker.Core.Triggers;

[TriggerMetadata("filesystem", "File System", "Fires when files or folders change")]
public class FileSystemTrigger : TriggerBase
{
    private FileSystemWatcher? _watcher;

    /// <summary>
    /// Path to watch. Can be a directory.
    /// </summary>
    public string Path { get; set; } = "";

    /// <summary>
    /// Filter pattern for files to watch (e.g., "*.txt", "*.json").
    /// Default is "*.*" (all files).
    /// </summary>
    public string Filter { get; set; } = "*.*";

    /// <summary>
    /// Whether to include subdirectories.
    /// </summary>
    public bool IncludeSubdirectories { get; set; } = false;

    /// <summary>
    /// What changes to watch for.
    /// </summary>
    public NotifyFilters NotifyFilter { get; set; } =
        NotifyFilters.FileName |
        NotifyFilters.DirectoryName |
        NotifyFilters.LastWrite |
        NotifyFilters.Size;

    /// <summary>
    /// Whether to trigger on file creation.
    /// </summary>
    public bool WatchCreated { get; set; } = true;

    /// <summary>
    /// Whether to trigger on file modification.
    /// </summary>
    public bool WatchChanged { get; set; } = true;

    /// <summary>
    /// Whether to trigger on file deletion.
    /// </summary>
    public bool WatchDeleted { get; set; } = true;

    /// <summary>
    /// Whether to trigger on file rename.
    /// </summary>
    public bool WatchRenamed { get; set; } = true;

    public FileSystemTrigger() : this(null)
    {
    }

    public FileSystemTrigger(ILogger? logger) : base(logger)
    {
        Name = "File System Trigger";
    }

    protected override void OnStart()
    {
        if (string.IsNullOrWhiteSpace(Path))
        {
            throw new InvalidOperationException("Path must be specified for FileSystemTrigger");
        }

        if (!Directory.Exists(Path) && !File.Exists(Path))
        {
            throw new InvalidOperationException($"Path does not exist: {Path}");
        }

        var watchPath = Directory.Exists(Path) ? Path : System.IO.Path.GetDirectoryName(Path);
        var watchFilter = Directory.Exists(Path) ? Filter : System.IO.Path.GetFileName(Path);

        _watcher = new FileSystemWatcher(watchPath!, watchFilter)
        {
            IncludeSubdirectories = IncludeSubdirectories,
            NotifyFilter = NotifyFilter,
            EnableRaisingEvents = true
        };

        if (WatchCreated) _watcher.Created += OnFileEvent;
        if (WatchChanged) _watcher.Changed += OnFileEvent;
        if (WatchDeleted) _watcher.Deleted += OnFileEvent;
        if (WatchRenamed) _watcher.Renamed += OnRenamedEvent;
        _watcher.Error += OnError;

        Logger.LogInformation(
            "File system trigger watching: {Path} (filter: {Filter}, subdirs: {SubDirs})",
            watchPath, watchFilter, IncludeSubdirectories);
    }

    protected override void OnStop()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= OnFileEvent;
            _watcher.Changed -= OnFileEvent;
            _watcher.Deleted -= OnFileEvent;
            _watcher.Renamed -= OnRenamedEvent;
            _watcher.Error -= OnError;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        if (!IsRunning) return;

        var data = new Dictionary<string, object?>
        {
            ["changeType"] = e.ChangeType.ToString(),
            ["fullPath"] = e.FullPath,
            ["name"] = e.Name,
            ["timestamp"] = DateTime.Now
        };

        Logger.LogDebug("File system event: {ChangeType} - {Path}", e.ChangeType, e.FullPath);
        RaiseFired(data);
    }

    private void OnRenamedEvent(object sender, RenamedEventArgs e)
    {
        if (!IsRunning) return;

        var data = new Dictionary<string, object?>
        {
            ["changeType"] = e.ChangeType.ToString(),
            ["fullPath"] = e.FullPath,
            ["name"] = e.Name,
            ["oldFullPath"] = e.OldFullPath,
            ["oldName"] = e.OldName,
            ["timestamp"] = DateTime.Now
        };

        Logger.LogDebug("File renamed: {OldPath} -> {NewPath}", e.OldFullPath, e.FullPath);
        RaiseFired(data);
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        Logger.LogError(e.GetException(), "File system watcher error");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _watcher?.Dispose();
            _watcher = null;
        }
        base.Dispose(disposing);
    }
}
