using System.Threading.Tasks;

namespace Compost.Core.Interfaces;

/// <summary>
/// Service for Git operations (Clone, Pull, Commit, Push)
/// </summary>
public interface IGitService
{
    /// <summary>
    /// Clones a repository to the local path
    /// </summary>
    Task<bool> CloneAsync(string remoteUrl, string localPath, string? branch = "main", string? pat = null);

    /// <summary>
    /// Pulls the latest changes from the remote
    /// </summary>
    Task<bool> PullAsync(string localPath, string? pat = null);

    /// <summary>
    /// Commits a file change and pushes to remote
    /// </summary>
    Task<string?> CommitAndPushAsync(string localPath, string relativeFilePath, string content, string message, string authorName, string authorEmail, string? pat = null);

    /// <summary>
    /// Checks if a repository is locally available and valid
    /// </summary>
    bool IsRepositoryValid(string localPath);

    /// <summary>
    /// Gets the current commit hash of the local repository
    /// </summary>
    string? GetCurrentCommitHash(string localPath);
}
