using Compost.Core.Models;

namespace Compost.Core.Interfaces;

public interface IGitCredentialProvider
{
    Task<GitCredential> GetDefaultCredentialAsync();
}
