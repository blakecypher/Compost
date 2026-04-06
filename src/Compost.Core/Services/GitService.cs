using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Compost.Core.Interfaces;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace Compost.Core.Services;

/// <summary>
/// Implementation of IGitService using LibGit2Sharp.
/// Note: Requires LibGit2Sharp package.
/// </summary>
public class GitService(ILogger<GitService> logger) : IGitService
{
    public Task<bool> CloneAsync(string remoteUrl, string localPath, string? branch = "main", string? pat = null)
    {
        try
        {
            if (Directory.Exists(localPath) && Directory.EnumerateFileSystemEntries(localPath).Any())
            {
                logger.LogWarning("Clone target directory not empty: {LocalPath}", localPath);
                return Task.FromResult(false);
            }

            var options = new CloneOptions
            {
                BranchName = branch ?? "main",
                Checkout = true,
                FetchOptions = { CredentialsProvider = CreateCredentialsProvider(pat) }
            };

            Repository.Clone(remoteUrl, localPath, options);
            logger.LogInformation("Successfully cloned {RemoteUrl} to {LocalPath}", remoteUrl, localPath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to clone repository {RemoteUrl}", remoteUrl);
            return Task.FromResult(false);
        }
    }

    public Task<bool> PullAsync(string localPath, string? pat = null)
    {
        try
        {
            using var repo = new Repository(localPath);
            var signature = new Signature("Compost Sync", "sync@compost.net", DateTimeOffset.Now);
            
            var options = new PullOptions
            {
                FetchOptions = { CredentialsProvider = CreateCredentialsProvider(pat) }
            };

            Commands.Pull(repo, signature, options);
            logger.LogInformation("Successfully pulled updates for {LocalPath}", localPath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to pull updates for {LocalPath}", localPath);
            return Task.FromResult(false);
        }
    }

    public async Task<string?> CommitAndPushAsync(string localPath, string relativeFilePath, string content, string message, string authorName, string authorEmail, string? pat = null)
    {
        try
        {
            using var repo = new Repository(localPath);
            
            // 1. Ensure file content is updated on disk
            var fullPath = Path.Combine(localPath, relativeFilePath);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            await File.WriteAllTextAsync(fullPath, content);
            
            // 2. Stage the file
            Commands.Stage(repo, relativeFilePath);
            
            // 3. Commit
            var author = new Signature(authorName, authorEmail, DateTimeOffset.Now);
            var committer = author;
            
            var commit = repo.Commit(message, author, committer);
            logger.LogInformation("Snippet committed to disk: {Hash} - {Message}", commit.Sha, message);
            
            // 4. Push
            var remote = repo.Network.Remotes["origin"];
            var options = new PushOptions
            {
                CredentialsProvider = CreateCredentialsProvider(pat)
            };
            
            repo.Network.Push(remote, @$"refs/heads/{repo.Head.FriendlyName}", options);
            logger.LogInformation("Changes pushed to remote for {LocalPath}", localPath);
            
            return commit.Sha;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to commit and push snippet at {LocalPath}/{FilePath}", localPath, relativeFilePath);
            return null;
        }
    }

    public bool IsRepositoryValid(string localPath)
    {
        try
        {
            return Repository.IsValid(localPath);
        }
        catch
        {
            return false;
        }
    }

    public GitStatus GetRepositoryStatus(string localPath)
    {
        try
        {
            if (!IsRepositoryValid(localPath)) return new GitStatus { Branch = "Not a Repository" };
            
            using var repo = new Repository(localPath);
            var status = new GitStatus
            {
                Branch = repo.Head.FriendlyName,
                CommitHash = repo.Head.Tip.Sha,
                HasUncommittedChanges = repo.RetrieveStatus().IsDirty
            };

            // Calculate ahead/behind if tracking a remote branch
            if (repo.Head.TrackingDetails != null)
            {
                status.Ahead = repo.Head.TrackingDetails.AheadBy ?? 0;
                status.Behind = repo.Head.TrackingDetails.BehindBy ?? 0;
            }

            return status;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get repository status for {LocalPath}", localPath);
            return new GitStatus { Branch = "Error" };
        }
    }

    private static LibGit2Sharp.Handlers.CredentialsHandler CreateCredentialsProvider(string? pat)
    {
        return (url, user, types) =>
            new UsernamePasswordCredentials
            {
                Username = pat ?? "token",
                Password = pat ?? string.Empty
            };
    }
}
