using Microsoft.Extensions.Logging;

namespace AionServer.Commons.Services;

public class FolderListenerService : IDisposable
{
    private readonly ILogger<FolderListenerService> _logger;
    private readonly FileSystemWatcher _watcher;
    
    private bool _disposed;

    public event FileSystemEventHandler? Changed;
    public event FileSystemEventHandler? Created;
    public event FileSystemEventHandler? Deleted;
    public event RenamedEventHandler? Renamed;

    public FolderListenerService(string path, ILogger<FolderListenerService> logger, string filter = "*.*")
    {
        _logger = logger;
        _watcher = new FileSystemWatcher(path)
        {
            Filter = filter,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnChanged;
        _watcher.Created += OnCreated;
        _watcher.Deleted += OnDeleted;
        _watcher.Renamed += OnRenamed;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("File changed: {FullPath}", e.FullPath);
        Changed?.Invoke(sender, e);
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("File/Directory created: {FullPath}", e.FullPath);
        Created?.Invoke(sender, e);
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("File/Directory deleted: {FullPath}", e.FullPath);
        Deleted?.Invoke(sender, e);
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        _logger.LogDebug("File/Directory renamed from {OldFullPath} to {FullPath}", e.OldFullPath, e.FullPath);
        Renamed?.Invoke(sender, e);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnChanged;
        _watcher.Created -= OnCreated;
        _watcher.Deleted -= OnDeleted;
        _watcher.Renamed -= OnRenamed;
        _watcher.Dispose();
        _disposed = true;
    }
}