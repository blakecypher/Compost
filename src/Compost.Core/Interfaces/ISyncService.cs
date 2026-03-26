namespace Compost.Core.Interfaces;

/// <summary>
/// Manages offline-first data synchronization between local SQLite and Cosmos DB
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Sync all data with the cloud
    /// </summary>
    Task<SyncResult> SyncAllAsync();

    /// <summary>
    /// Sync a specific entity type
    /// </summary>
    Task<SyncResult> SyncEntityTypeAsync<T>() where T : class;

    /// <summary>
    /// Get sync status
    /// </summary>
    Task<SyncStatus> GetSyncStatusAsync();

    /// <summary>
    /// Check if device is online
    /// </summary>
    bool IsOnline();

    /// <summary>
    /// Get last successful sync timestamp
    /// </summary>
    Task<DateTime?> GetLastSyncTimeAsync();

    /// <summary>
    /// Get pending changes count (not yet synced to cloud)
    /// </summary>
    Task<int> GetPendingChangesCountAsync();

    /// <summary>
    /// Force push local changes to cloud (conflict resolution: last-write-wins)
    /// </summary>
    Task ForcePushAsync();

    /// <summary>
    /// Force pull from cloud (overwrite local changes)
    /// </summary>
    Task ForcePullAsync();

    /// <summary>
    /// Register for sync status change notifications
    /// </summary>
    event EventHandler<SyncStatusChangedEventArgs>? SyncStatusChanged;
}

public class SyncResult
{
    public bool Success { get; set; }
    public int ItemsSynced { get; set; }
    public int ItemsFailed { get; set; }
    public List<string> Errors { get; set; } = [];
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}

public class SyncStatus
{
    public bool IsSyncing { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public int PendingChanges { get; set; }
    public SyncState State { get; set; }
}

public class SyncStatusChangedEventArgs : EventArgs
{
    public SyncStatus Status { get; set; } = new();
}

public enum SyncState
{
    Idle,
    Syncing,
    Error,
    Offline
}
